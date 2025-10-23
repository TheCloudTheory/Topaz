using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
public sealed class CreateEventHubNamespaceCommand(ITopazLogger logger) : Command<CreateEventHubNamespaceCommand.CreateEventHubCommandSettings>
{
    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(CreateEventHubNamespaceCommand)}.{nameof(Execute)}.");
        
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(Guid.Parse(settings.SubscriptionId)), resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }

        var controlPlane = new EventHubServiceControlPlane(new ResourceProvider(logger), logger);
        var request = new CreateOrUpdateEventHubNamespaceRequest();
        var ns = controlPlane.CreateOrUpdateNamespace(resourceGroup.resource.GetSubscription(), resourceGroupIdentifier,
            settings.Location!, EventHubNamespaceIdentifier.From(settings.Name!), request);

        logger.LogInformation(ns.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Resource group location can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }
        
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}