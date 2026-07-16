using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Deployments.Core.Components;
using Azure.Deployments.Core.Configuration;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Diagnostics;
using Azure.Deployments.Core.ErrorResponses;
using Azure.Deployments.Expression.Engines;
using Azure.Deployments.Expression.Expressions;
using Azure.Deployments.Templates.Engines;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class ArmTemplateEngineFacade(ITopazLogger logger)
{
    public Template Parse(string input)
    {
        var template = TemplateParsingEngine.ParseTemplate(input);
        return template;
    }

    public void ProcessTemplate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, Template template,
        InsensitiveDictionary<JToken> metadataInsensitive, JsonElement? propertiesParameters)
    {
        var inputParameters = BuildInputParameters(propertiesParameters);

        TemplateEngine.ProcessTemplateLanguageExpressions("topaz", subscriptionIdentifier.Value.ToString(),
            resourceGroupIdentifier.Value, template, "", inputParameters!,
            metadataInsensitive,
            new PreprocessingTemplateExtensionResolver(template, null, null,
                new FactBasedExtensionConfigSchemaDirectoryFactory().GetOrCreateDirectory()),
            new TemplateMetricsRecorder(), InsensitiveDictionary<JToken>.Empty);
    }

    /// <summary>
    /// Processes ARM template language expressions at subscription scope.
    /// Subscription-scoped functions such as <c>subscription()</c> and <c>tenant()</c> are evaluated;
    /// <c>resourceGroup()</c> is not available at this scope and will not be resolved.
    /// </summary>
    public void ProcessTemplateAtSubscriptionScope(SubscriptionIdentifier subscriptionIdentifier,
        Template template, InsensitiveDictionary<JToken> metadataInsensitive, JsonElement? propertiesParameters)
    {
        var inputParameters = BuildInputParameters(propertiesParameters);

        TemplateEngine.ProcessTemplateLanguageExpressions("topaz", subscriptionIdentifier.Value.ToString(),
            "", template, "", inputParameters!,
            metadataInsensitive,
            new PreprocessingTemplateExtensionResolver(template, null, null,
                new FactBasedExtensionConfigSchemaDirectoryFactory().GetOrCreateDirectory()),
            new TemplateMetricsRecorder(), InsensitiveDictionary<JToken>.Empty);
    }

    private static InsensitiveDictionary<JToken> BuildInputParameters(JsonElement? propertiesParameters)
    {
        if (propertiesParameters == null ||
            propertiesParameters.Value.ValueKind != JsonValueKind.Object)
            return InsensitiveDictionary<JToken>.Empty;

        var dict = propertiesParameters.Value.Deserialize<Dictionary<string, CreateDeploymentRequest.ParameterValue>>(GlobalSettings.JsonOptions);
        if (dict == null || dict.Count == 0)
            return InsensitiveDictionary<JToken>.Empty;

        return dict.ToInsensitiveDictionary(meta => meta.Key, meta => JToken.Parse(meta.Value.ToString()));
    }

    /// <summary>
    /// Processes ARM template language expressions at tenant scope.
    /// Only tenant-scoped functions such as <c>tenant()</c> are evaluated;
    /// <c>subscription()</c> and <c>resourceGroup()</c> are not available at this scope.
    /// </summary>
    public void ProcessTemplateAtTenantScope(
        Template template, InsensitiveDictionary<JToken> metadataInsensitive, JsonElement? propertiesParameters)
    {
        var inputParameters = BuildInputParameters(propertiesParameters);

        TemplateEngine.ProcessTemplateLanguageExpressions("topaz", "",
            "", template, "", inputParameters!,
            metadataInsensitive,
            new PreprocessingTemplateExtensionResolver(template, null, null,
                new FactBasedExtensionConfigSchemaDirectoryFactory().GetOrCreateDirectory()),
            new TemplateMetricsRecorder(), InsensitiveDictionary<JToken>.Empty);
    }

    /// <summary>
    /// Processes ARM template language expressions at management group scope.
    /// Only management group-scoped functions such as <c>tenant()</c> are evaluated;
    /// <c>subscription()</c> and <c>resourceGroup()</c> are not available at this scope.
    /// Uses the same processing as tenant scope (no subscription or resource group context).
    /// </summary>
    public void ProcessTemplateAtManagementGroupScope(
        Template template, InsensitiveDictionary<JToken> metadataInsensitive, JsonElement? propertiesParameters)
    {
        // Management group scope evaluation is equivalent to tenant scope
        ProcessTemplateAtTenantScope(template, metadataInsensitive, propertiesParameters);
    }

    public void Validate(Template template)
    {
        TemplateEngine.ValidateTemplate(template, "apiVersion", TemplateDeploymentScope.ResourceGroup);
    }

    /// <summary>
    /// Evaluates any remaining ARM expression strings inside a resource's <c>properties</c>
    /// JObject in-place, using the expression context built from the already-processed
    /// <paramref name="template"/>. This handles properties whose values are variable
    /// references (e.g. <c>[variables('locations')]</c>) that the template engine resolves
    /// lazily and does not inline into the raw properties JToken.
    /// </summary>
    public void EvaluateResourceProperties(
        string subscriptionId, string resourceGroupName,
        Template template, TemplateResource resource)
    {
        if (resource.Properties?.Value is not JObject properties)
            return;

        var metrics = new TemplateMetricsRecorder();
        var evalCtx = TemplateEngine.GetExpressionEvaluationContext(
            string.Empty, subscriptionId, resourceGroupName, template, metrics,
            false);

        EvaluateJTokenExpressions(properties, evalCtx);
    }

    private void EvaluateJTokenExpressions(JToken token, IEvaluationContext evalCtx)
    {
        switch (token)
        {
            case JObject obj:
            {
                foreach (var prop in obj.Properties().ToList())
                {
                    if (prop.Value.Type == JTokenType.String)
                    {
                        var s = prop.Value.Value<string>()!;
                        if (!ExpressionsEngine.IsLanguageExpression(s)) continue;

                        try
                        {
                            var evaluated = ExpressionsEngine.EvaluateLanguageExpression(
                                s, evalCtx, new TemplateErrorAdditionalInfo());
                            prop.Value = evaluated;
                        }
                        catch
                        {
                            logger.LogError("Failed to evaluate expression: " + s + "");
                        }
                    }
                    else
                    {
                        EvaluateJTokenExpressions(prop.Value, evalCtx);
                    }
                }

                break;
            }
            case JArray arr:
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i].Type == JTokenType.String)
                    {
                        var s = arr[i].Value<string>()!;
                        if (!ExpressionsEngine.IsLanguageExpression(s)) continue;
                        try
                        {
                            var evaluated = ExpressionsEngine.EvaluateLanguageExpression(
                                s, evalCtx, new TemplateErrorAdditionalInfo());
                            arr[i] = evaluated;
                        }
                        catch
                        {
                            logger.LogError("Failed to evaluate expression: " + s + "");
                        }
                    }
                    else
                    {
                        EvaluateJTokenExpressions(arr[i], evalCtx);
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    /// Evaluates ARM expressions in each output value using the expression evaluation context
    /// built from the already-processed <paramref name="template"/>.
    /// Output values that are plain literals are returned as-is; ARM expressions
    /// (e.g. <c>[parameters('x')]</c>) are evaluated to their resolved values.
    /// When the Azure SDK expression engine cannot evaluate an expression (e.g. <c>reference()</c>),
    /// <paramref name="referenceResolver"/> is tried before falling back to <c>null</c>.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID used to build the expression evaluation context.</param>
    /// <param name="resourceGroupName">The resource group name used to build the expression evaluation context.</param>
    /// <param name="template">The already-processed ARM template whose outputs are to be evaluated.</param>
    /// <param name="armLogger">Logger used to emit warnings when an output expression cannot be evaluated.</param>
    /// <param name="referenceResolver">
    /// Optional delegate that attempts to resolve a <c>reference()</c> ARM expression to a
    /// <see cref="JToken"/> when the Azure SDK expression engine cannot evaluate it (e.g. <c>listKeys</c>,
    /// <c>reference()</c>). Receives the raw ARM expression string and returns the resolved value, or
    /// <c>null</c> if it cannot be resolved.
    /// </param>
    /// <param name="symbolicNameMap">
    /// Optional mapping from Bicep symbolic resource names (dict-key in newer ARM templates)
    /// to their resource type. Enables resolution of <c>reference('symbolicName')</c>
    /// expressions that the ARM engine cannot resolve at output-evaluation time.
    /// </param>
    public JObject EvaluateOutputs(
        string subscriptionId,
        string resourceGroupName,
        Template template,
        ITopazLogger armLogger,
        Func<string, JToken?>? referenceResolver = null,
        IReadOnlyDictionary<string, string>? symbolicNameMap = null)
    {
        var metrics = new TemplateMetricsRecorder();
        var evalCtx = TemplateEngine.GetExpressionEvaluationContext(
            string.Empty, subscriptionId, resourceGroupName, template, metrics,
            false);

        var result = new JObject();
        foreach (var kv in template.Outputs)
        {
            var entry = new JObject
            {
                ["type"] = kv.Value.Type?.Value.ToString().ToLowerInvariant() ?? "object"
            };

            var rawJToken = kv.Value.Value?.Value;
            if (rawJToken != null)
            {
                var rawString = rawJToken.Type == JTokenType.String ? rawJToken.Value<string>() : null;
                if (rawString != null && ExpressionsEngine.IsLanguageExpression(rawString))
                {
                    try
                    {
                        var evaluated = ExpressionsEngine.EvaluateLanguageExpression(
                            rawString, evalCtx, new TemplateErrorAdditionalInfo());
                        entry["value"] = evaluated;
                    }
                    catch (Exception ex)
                    {
                        // Some ARM functions (e.g. listKeys, reference()) are not supported in the
                        // output evaluation context. Attempt the reference() resolver first, then
                        // return null rather than crashing the host process.
                        JToken? resolved = null;
                        if (referenceResolver != null && rawString.Contains("reference(", StringComparison.OrdinalIgnoreCase))
                        {
                            // Pre-substitute parameters('name') and variables('name') → 'value'
                            // so the resolver sees a literal name even when the output uses a
                            // parametrised or variable-based resourceId().
                            var preResolved = ParametersCallPattern.Replace(rawString, m =>
                            {
                                var paramExpr = $"[parameters('{m.Groups[1].Value}')]";
                                try
                                {
                                    var val = ExpressionsEngine.EvaluateLanguageExpression(
                                        paramExpr, evalCtx, new TemplateErrorAdditionalInfo());
                                    return val is JValue jv && jv.Value is string s ? $"'{s}'" : m.Value;
                                }
                                catch { return m.Value; }
                            });
                            preResolved = VariablesCallPattern.Replace(preResolved, m =>
                            {
                                var varExpr = $"[variables('{m.Groups[1].Value}')]";
                                try
                                {
                                    var val = ExpressionsEngine.EvaluateLanguageExpression(
                                        varExpr, evalCtx, new TemplateErrorAdditionalInfo());
                                    return val is JValue jv && jv.Value is string s ? $"'{s}'" : m.Value;
                                }
                                catch { return m.Value; }
                            });
                            resolved = referenceResolver(preResolved);

                            // If the whole expression wasn't a reference() call (e.g. it is
                            // wrapped in format()), try substituting every reference(...)
                            // sub-expression with its resolved string value and re-evaluating.
                            if (resolved == null)
                            {
                                // If there are symbolic name references (reference('symbolName'))
                                // expand them to resource-ID form using the symbolic name map.
                                var withSymbolsExpanded = preResolved;
                                if (symbolicNameMap != null)
                                {
                                    withSymbolsExpanded = SymbolicReferencePattern.Replace(preResolved, m =>
                                    {
                                        var sym = m.Groups[1].Value;
                                        var propPath = m.Groups[2].Value; // e.g. ".defaultHostName"
                                        if (!symbolicNameMap.TryGetValue(sym, out var resourceType))
                                            return m.Value;
                                        // Find the resolved resource name in the template
                                        var match = template.Resources.FirstOrDefault(r =>
                                            string.Equals(r.Type?.Value?.ToString(), resourceType,
                                                StringComparison.OrdinalIgnoreCase));
                                        if (match?.Name?.Value == null) return m.Value;
                                        var resolvedName = match.Name.Value!;
                                        return $"reference(resourceId('{resourceType}', '{resolvedName}')){propPath}";
                                    });
                                }

                                var withRefSubstituted = ReferenceSubExprPattern.Replace(withSymbolsExpanded, m =>
                                {
                                    var refExpr = m.Value;
                                    // Wrap in [...] so TryResolve can strip the brackets
                                    var inner = refExpr.StartsWith('[') ? refExpr : $"[{refExpr}]";
                                    var refVal = referenceResolver(inner);
                                    return refVal is JValue jvr && jvr.Value is string sr ? $"'{sr}'" : refExpr;
                                });
                                if (!string.Equals(withRefSubstituted, preResolved, StringComparison.Ordinal))
                                {
                                    try
                                    {
                                        resolved = ExpressionsEngine.EvaluateLanguageExpression(
                                            withRefSubstituted, evalCtx, new TemplateErrorAdditionalInfo());
                                    }
                                    catch { /* leave resolved as null */ }
                                }
                            }
                        }

                        if (resolved != null)
                            entry["value"] = resolved;
                        else
                        {
                            armLogger.LogWarning($"ARM output '{kv.Key}' could not be evaluated: {ex.Message}");
                            entry["value"] = null;
                        }
                    }
                }
                else
                {
                    entry["value"] = rawJToken;
                }
            }

            result[kv.Key] = entry;
        }
        return result;
    }

    /// <summary>
    /// Replaces <c>parameters('name')</c> sub-expressions within an ARM expression string
    /// with their single-quoted literal values, enabling the reference() resolver to
    /// match resource names even when outputs use parametrised <c>resourceId()</c> calls.
    /// Any parameter whose value cannot be evaluated is left as-is.
    /// </summary>
    private static readonly Regex ParametersCallPattern = new(
        @"parameters\('([^']+)'\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VariablesCallPattern = new(
        @"variables\('([^']+)'\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SymbolicReferencePattern = new(
        @"reference\('([^']+)'\)((?:\.[a-zA-Z0-9_]+)+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>reference(...)</c> sub-expression (with an optional property
    /// accessor suffix like <c>.defaultHostName</c>) that may appear nested within
    /// a larger ARM expression such as <c>format('https://{0}', reference(...).prop)</c>.
    /// Uses a simple heuristic: matches <c>reference(</c> followed by everything up to
    /// the last <c>)</c> that still has a trailing <c>.propertyName</c> accessor.
    /// </summary>
    private static readonly Regex ReferenceSubExprPattern = new(
        @"reference\((?:[^()]*|\((?:[^()]*|\([^()]*\))*\))*\)(?:\.[a-zA-Z0-9_]+)+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
}