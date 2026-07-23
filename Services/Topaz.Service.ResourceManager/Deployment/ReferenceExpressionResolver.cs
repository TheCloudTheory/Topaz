using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Resolves ARM <c>reference(resourceId(...))</c> expressions in deployment outputs by reading
/// the deployed resource's persisted <c>metadata.json</c> from the Topaz filesystem store.
/// </summary>
internal static class ReferenceExpressionResolver
{
    // Maps lowercase ARM resource type → service directory segment inside the resource-group folder.
    private static readonly Dictionary<string, string> ServiceDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["microsoft.keyvault/vaults"]                          = ".azure-key-vault",
        ["microsoft.storage/storageaccounts"]                  = ".azure-storage",
        ["microsoft.eventhub/namespaces"]                      = ".azure-event-hub",
        ["microsoft.servicebus/namespaces"]                    = ".service-bus",
        ["microsoft.compute/virtualmachines"]                  = ".virtual-machine",
        ["microsoft.compute/disks"]                            = ".managed-disk",
        ["microsoft.network/virtualnetworks"]                  = ".azure-virtual-network",
        ["microsoft.network/networkinterfaces"]                = ".azure-nic",
        ["microsoft.network/privateendpoints"]                 = ".private-endpoint",
        ["microsoft.network/networksecuritygroups"]            = ".azure-nsg",
        ["microsoft.network/publicipaddresses"]                = ".azure-pip",
        ["microsoft.network/loadbalancers"]                    = ".load-balancer",
        ["microsoft.containerregistry/registries"]             = ".container-registry",
        ["microsoft.managedidentity/userassignedidentities"]   = ".managed-identity",
        ["microsoft.documentdb/databaseaccounts"]              = ".azure-cosmos-db",
        ["microsoft.sql/servers"]                              = ".azure-sql",
        ["microsoft.web/serverfarms"]                          = ".azure-web-plans",
        ["microsoft.web/sites"]                                = ".azure-web-sites",
    };

    // reference(resourceId('TYPE', 'NAME'), 'API-VERSION?').propPath  — 2-arg resourceId
    private static readonly Regex ResourceId2ArgPattern = new(
        @"reference\(resourceId\('([^']+)',\s*'([^']+)'\)(?:,\s*'[^']*')?\)(.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // reference(resourceId('SUB', 'RG', 'TYPE', 'NAME'), 'API-VERSION?').propPath  — 4-arg resourceId
    private static readonly Regex ResourceId4ArgPattern = new(
        @"reference\(resourceId\('[^']*',\s*'[^']*',\s*'([^']+)',\s*'([^']+)'\)(?:,\s*'[^']*')?\)(.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // reference(extensionResourceId(scope, 'TYPE', 'NAME'), 'API-VERSION?').propPath
    private static readonly Regex ExtensionResourceIdPattern = new(
        @"reference\(extensionResourceId\([^,)]*,\s*'([^']+)',\s*'([^']+)'\)(?:,\s*'[^']*')?\)(.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Attempts to resolve a <c>reference()</c> ARM expression by reading the deployed resource
    /// from the Topaz filesystem store and navigating the specified property path.
    /// Returns <c>null</c> when the expression cannot be resolved (unknown type, missing file, etc.).
    /// </summary>
    /// <remarks>
    /// <para>Supported patterns:</para>
    /// <list type="bullet">
    ///   <item><c>reference(resourceId('TYPE', 'NAME'), 'API-VERSION').prop</c></item>
    ///   <item><c>reference(resourceId('SUB', 'RG', 'TYPE', 'NAME'), 'API-VERSION').prop</c> — sub/rg args are ignored; the calling deployment's sub/rg is used</item>
    ///   <item><c>reference(extensionResourceId(scope, 'TYPE', 'NAME')).prop</c> — scope arg is ignored</item>
    /// </list>
    /// <para>
    /// Expressions where <c>reference()</c> is nested inside another function such as
    /// <c>concat(reference(...).prop, '-suffix')</c> are not supported and return <c>null</c>.
    /// </para>
    /// </remarks>
    public static JToken? TryResolve(
        string expression,
        string subscriptionId,
        string resourceGroupName,
        string baseEmulatorPath)
    {
        // Strip outer [ ] so the regex works on the bare function call
        var inner = expression.Trim();
        if (inner.StartsWith('[') && inner.EndsWith(']'))
            inner = inner[1..^1].Trim();

        // Try 4-arg first (more specific) to avoid partial match by the 2-arg pattern
        if (TryMatchPattern(ResourceId4ArgPattern, inner, out var type4, out var name4, out var path4))
            return ResolveResource(type4!, name4!, path4!, subscriptionId, resourceGroupName, baseEmulatorPath);

        if (TryMatchPattern(ResourceId2ArgPattern, inner, out var type2, out var name2, out var path2))
            return ResolveResource(type2!, name2!, path2!, subscriptionId, resourceGroupName, baseEmulatorPath);

        if (TryMatchPattern(ExtensionResourceIdPattern, inner, out var typeE, out var nameE, out var pathE))
            return ResolveResource(typeE!, nameE!, pathE!, subscriptionId, resourceGroupName, baseEmulatorPath);

        return null;
    }

    private static bool TryMatchPattern(Regex pattern, string expression, out string? type, out string? name, out string? propertyPath)
    {
        var match = pattern.Match(expression);
        if (!match.Success)
        {
            type = name = propertyPath = null;
            return false;
        }
        type = match.Groups[1].Value;
        name = match.Groups[2].Value;
        propertyPath = match.Groups[3].Value;
        return true;
    }

    private static JToken? ResolveResource(
        string resourceType,
        string resourceName,
        string propertyPath,
        string subscriptionId,
        string resourceGroupName,
        string baseEmulatorPath)
    {
        if (!ServiceDirectories.TryGetValue(resourceType, out var serviceDir))
            return null;

        var metadataPath = FindMetadataFile(baseEmulatorPath, subscriptionId, resourceGroupName, serviceDir, resourceName);
        if (metadataPath == null || !File.Exists(metadataPath))
            return null;

        try
        {
            var json = File.ReadAllText(metadataPath);
            var resource = JObject.Parse(json);

            // Navigate property path — e.g. ".properties.vaultUri" → ["properties"]["vaultUri"]
            var segments = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            JToken? current = resource;
            foreach (var segment in segments)
            {
                if (current is JObject obj && obj.TryGetValue(segment, StringComparison.OrdinalIgnoreCase, out var next))
                {
                    current = next;
                }
                else
                {
                    // In real Azure, reference() returns the resource's properties object, so
                    // "reference(...).principalId" maps to metadata.json → properties.principalId.
                    // If the top-level lookup fails, retry from the properties sub-object.
                    if (current == resource &&
                        resource.TryGetValue("properties", StringComparison.OrdinalIgnoreCase, out var props) &&
                        props is JObject propsObj &&
                        propsObj.TryGetValue(segment, StringComparison.OrdinalIgnoreCase, out var propsNext))
                    {
                        current = propsNext;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Constructs the full path to <c>metadata.json</c> by resolving each path segment
    /// case-insensitively, matching <see cref="Topaz.Service.Shared.ResourceProviderBase{TService}"/> behaviour.
    /// </summary>
    private static string? FindMetadataFile(
        string baseEmulatorPath,
        string subscriptionId,
        string resourceGroupName,
        string serviceDir,
        string resourceName)
    {
        // .topaz / .subscription / {subscriptionId} / .resource-group / {resourceGroup} / {serviceDir} / {resourceName} / metadata.json
        var subRoot   = ResolvePathSegment(baseEmulatorPath, ".subscription");
        if (subRoot == null) return null;
        var subId     = ResolvePathSegment(subRoot, subscriptionId);
        if (subId == null) return null;
        var rgRoot    = ResolvePathSegment(subId, ".resource-group");
        if (rgRoot == null) return null;
        var rg        = ResolvePathSegment(rgRoot, resourceGroupName);
        if (rg == null) return null;
        var svc       = ResolvePathSegment(rg, serviceDir);
        if (svc == null) return null;
        var resource  = ResolvePathSegment(svc, resourceName);
        if (resource == null) return null;

        return Path.Combine(resource, "metadata.json");
    }

    private static string? ResolvePathSegment(string parentPath, string segment)
    {
        var exact = Path.Combine(parentPath, segment);
        if (Directory.Exists(exact)) return exact;
        if (!Directory.Exists(parentPath)) return null;

        return Directory.EnumerateDirectories(parentPath)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), segment, StringComparison.OrdinalIgnoreCase));
    }
}
