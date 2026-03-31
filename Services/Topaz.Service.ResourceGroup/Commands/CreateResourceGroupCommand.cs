using JetBrains.Annotations;
using Topaz.Documentation.Command;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group create", "group", "Creates a new resource group.")]
[CommandExample("Create a resource group", "topaz group create \\\n    --name \"my-rg\" \\\n    --location \"eastus\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CreateResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<CreateResourceGroupCommand.CreateResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(CreateResourceGroupCommand), nameof(Execute), "Creating a resource group...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.Name!);
        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var operation = controlPlane.Create(subscriptionIdentifier, resourceGroupIdentifier, settings.Location!);

        if (operation.Result != OperationResult.Created)
        {
            logger.LogError(operation.ToString());
            return 1;
        }
        
        logger.LogInformation(operation.Resource!.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateResourceGroupCommandSettings settings)
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

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Azure region for the resource group.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
