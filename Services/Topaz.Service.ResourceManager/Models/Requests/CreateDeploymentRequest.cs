using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Templates.Engines;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Requests;

internal record CreateDeploymentRequest
{
    public string? Location { get; init; }
    public DeploymentProperties? Properties { get; init; }

    internal record DeploymentProperties
    {
        public string? Mode { get; init; }
        public object? Template { get; set; }

        /// <summary>
        /// Raw JSON for the deployment parameters — either inline format
        /// <c>{"param1":{"value":"..."}}</c> or parameter-file format
        /// <c>{"$schema":"...","parameters":{"param1":{"value":"..."}}}</c>.
        /// Use <see cref="GetParameterValues"/> to extract the flat dictionary.
        /// </summary>
        public JsonElement? Parameters { get; set; }

        public Dictionary<string, ParameterValue>? GetParameterValues()
        {
            if (Parameters == null || Parameters.Value.ValueKind != JsonValueKind.Object)
                return null;

            var element = Parameters.Value;

            // Parameter-file format: {"$schema":"...","contentVersion":"...","parameters":{...}}
            if (element.TryGetProperty("parameters", out var nestedParameters) &&
                nestedParameters.ValueKind == JsonValueKind.Object)
            {
                return nestedParameters.Deserialize<Dictionary<string, ParameterValue>>(GlobalSettings.JsonOptions);
            }

            // Inline format: {"param1":{"value":"..."}, ...}
            return element.Deserialize<Dictionary<string, ParameterValue>>(GlobalSettings.JsonOptions);
        }
    }

    internal record DeploymentParameters
    {
        [UsedImplicitly] public string? Schema { get; set; }
        [UsedImplicitly] public string? ContentVersion { get; set; }
        public Dictionary<string, ParameterValue>? Parameters { get; set; }
    }

    internal record ParameterValue
    {
        public object? Value { get; set; }

        public override string ToString() => Value == null ? "null" : System.Text.Json.JsonSerializer.Serialize(Value);
    }

    public Template ToTemplate()
    {
        var templateJson = Properties?.Template == null
            ? throw new InvalidOperationException("Deployment template is missing.")
            : Properties.Template.ToString();

        return string.IsNullOrWhiteSpace(templateJson)
            ? throw new InvalidOperationException("Deployment template is empty.")
            : TemplateParsingEngine.ParseTemplate(templateJson);
    }
}