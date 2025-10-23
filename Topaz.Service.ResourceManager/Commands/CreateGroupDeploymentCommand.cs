using System.Reflection;
using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
public class CreateGroupDeploymentCommand(ITopazLogger logger) : Command<CreateGroupDeploymentCommand.CreateGroupDeploymentCommandSettings>
{
    public override int Execute(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateGroupDeploymentCommand)}.{nameof(Execute)}.");

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }
        
        var controlPlane = new ResourceManagerControlPlane(new ResourceManagerResourceProvider(logger));
        var fakeRequest = GetEmptyTemplate();

        var deploymentName = string.IsNullOrWhiteSpace(settings.Name) ? "empty-template" : settings.Name;
        var deployment = controlPlane.CreateOrUpdateDeployment(resourceGroup.resource.GetSubscription(),
            resourceGroupIdentifier, deploymentName, fakeRequest,
            resourceGroup.resource.Location, settings.Mode.ToString());

        logger.LogInformation(deployment.resource.ToString());

        return 0;
    }

    private static string GetEmptyTemplate()
    {
        var emptyTemplate = Assembly.GetExecutingAssembly()
            ?.GetManifestResourceStream("Topaz.Service.ResourceManager.empty-template.json");
        if (emptyTemplate is null) throw new InvalidOperationException();

        using var reader = new StreamReader(emptyTemplate);
        return reader.ReadToEnd();
    }

    public override ValidationResult Validate(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }
        
        return string.IsNullOrEmpty(settings.ResourceGroup) ? ValidationResult.Error("Resource group name is required when creating a group deployment.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateGroupDeploymentCommandSettings : CommandSettings
    {
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;
    }

    public enum DeploymentMode
    {
        Incremental,
        Complete
    }
}