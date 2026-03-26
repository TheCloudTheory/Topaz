using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Azure.Deployments.Templates.Engines;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Topaz.Service.ResourceManager.Models.Requests;

internal record CreateDeploymentRequest
{
    public DeploymentProperties? Properties { get; init; }

    internal record DeploymentProperties
    {
        public string? Mode { get; init; }
        public object? Template { get; set; }
        public DeploymentParameters? Parameters { get; set; }
    }

    internal record DeploymentParameters
    {
        public string? Schema { get; set; }
        public string? ContentVersion { get; set; }
        public Dictionary<string, ParameterValue>? Parameters { get; set; }
    }

    internal record ParameterValue
    {
        public object? Value { get; set; }

        public override string ToString() => Value == null ? "null" : JsonSerializer.Serialize(Value);
    }

    public Template ToTemplate()
    {
        var templateJson = Properties?.Template == null
            ? throw new InvalidOperationException("Deployment template is missing.")
            : Properties.Template.ToString();

        return TemplateParsingEngine.ParseTemplate(templateJson);
    }
}