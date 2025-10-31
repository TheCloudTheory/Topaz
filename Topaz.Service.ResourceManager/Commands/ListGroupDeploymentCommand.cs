using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
[CommandDefinition("deployment group list", "deployment", "Returns a list of all deployments for the given resource group")]
public class ListGroupDeploymentCommand(ITopazLogger logger) : Command<ListGroupDeploymentCommand.ListGroupDeploymentCommandSettings>
{
    public override int Execute(CommandContext context, ListGroupDeploymentCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(ListGroupDeploymentCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound || resourceGroupOperation.Resource == null)
        {
            logger.LogError(resourceGroupOperation.ToString());
            return 1;
        }
        
        var controlPlane = new ResourceManagerControlPlane(new ResourceManagerResourceProvider(logger), new TemplateDeploymentOrchestrator(logger));
        var deployments = controlPlane.GetDeployments(subscriptionIdentifier, resourceGroupIdentifier);
        
        logger.LogInformation(JsonSerializer.Serialize(deployments.resource));

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListGroupDeploymentCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Group deployment subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Group deployment subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.ResourceGroup) ? ValidationResult.Error("Resource group name is required when creating a group deployment.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class ListGroupDeploymentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("Subscription ID for the deployment", true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Resource group for the deployment", true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}