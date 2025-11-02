using System.Collections.ObjectModel;
using Azure.Deployments.Core.Configuration;
using Azure.Deployments.Core.Definitions.Extensibility;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Diagnostics;
using Azure.Deployments.Templates.Engines;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ResourceManager;

internal sealed class ArmTemplateEngineFacade
{
    public Template Parse(string input)
    {
        var template = TemplateParsingEngine.ParseTemplate(input);
        return template;
    }

    public void ProcessTemplate(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, Template template)
    {
        TemplateEngine.ProcessTemplateLanguageExpressions("topaz", subscriptionIdentifier.Value.ToString(),
            resourceGroupIdentifier.Value, template, "", InsensitiveDictionary<JToken>.Empty,
            InsensitiveDictionary<JToken>.Empty,
            new ReadOnlyDictionary<string, IReadOnlyDictionary<string, DeploymentExtensionConfigItem>>(
                new Dictionary<string, IReadOnlyDictionary<string, DeploymentExtensionConfigItem>>()),
            new TemplateMetricsRecorder(), InsensitiveDictionary<JToken>.Empty);
    }

    public void Validate(Template template)
    {
        TemplateEngine.ValidateTemplate(template, "apiVersion", TemplateDeploymentScope.ResourceGroup);
    }
}