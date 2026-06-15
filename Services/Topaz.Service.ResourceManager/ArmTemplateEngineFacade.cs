using Azure.Deployments.Core.Components;
using Azure.Deployments.Core.Configuration;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Diagnostics;
using Azure.Deployments.Core.ErrorResponses;
using Azure.Deployments.Expression.Engines;
using Azure.Deployments.Templates.Engines;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class ArmTemplateEngineFacade
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
    /// Evaluates ARM expressions in each output value using the expression evaluation context
    /// built from the already-processed <paramref name="template"/>.
    /// Output values that are plain literals are returned as-is; ARM expressions
    /// (e.g. <c>[parameters('x')]</c>) are evaluated to their resolved values.
    /// </summary>
    public JObject EvaluateOutputs(string subscriptionId, string resourceGroupName, Template template, ITopazLogger logger)
    {
        var metrics = new TemplateMetricsRecorder();
        var evalCtx = TemplateEngine.GetExpressionEvaluationContext(
            string.Empty, subscriptionId, resourceGroupName, template, metrics,
            false, null, null, null, null, null, null);

        var result = new JObject();
        foreach (var kv in template.Outputs)
        {
            var entry = new JObject();
            entry["type"] = kv.Value.Type?.Value.ToString().ToLowerInvariant() ?? "object";

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
                        // Some ARM functions (e.g. listKeys) are not supported in the
                        // output evaluation context. Return null for those outputs rather
                        // than crashing the host process.
                        logger.LogWarning($"ARM output '{kv.Key}' could not be evaluated: {ex.Message}");
                        entry["value"] = null;
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
}