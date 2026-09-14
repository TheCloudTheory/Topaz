using System.Text;

namespace Topaz.ResourceManager;

public static class ArmResourceId
{
    /// <summary>
    /// Constructs an Azure Resource Manager (ARM) resource ID based on the provided scope, type, and name.
    /// The resulting ID adheres to the ARM resource ID structure, including handling of provider namespaces
    /// and paired type/name segments.
    /// </summary>
    /// <param name="scope">The base scope of the resource, such as a subscription or resource group path.</param>
    /// <param name="type">The type of the resource, including provider namespace and resource types (e.g., "Microsoft.Storage/storageAccounts").</param>
    /// <param name="name">The name of the resource or hierarchy of resource names for nested resources.</param>
    /// <returns>A fully qualified ARM resource ID as a string.</returns>
    public static string Build(string scope, string? type, string? name)
    {
        var typeSegments = (type ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var nameSegments = (name ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

        // typeSegments[0] is the provider namespace; the rest pair up with the name segments.
        // Anything that does not pair is left as it was rather than guessed at.
        if (typeSegments.Length < 2 || typeSegments.Length - 1 != nameSegments.Length)
            return $"{scope}/providers/{type}/{name}";

        var id = new StringBuilder(scope).Append("/providers/").Append(typeSegments[0]);
        for (var i = 0; i < nameSegments.Length; i++)
            id.Append('/').Append(typeSegments[i + 1]).Append('/').Append(nameSegments[i]);

        return id.ToString();
    }
}
