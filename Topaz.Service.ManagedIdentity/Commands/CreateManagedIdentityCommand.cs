using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity create", "managed-identity", "Creates a new user-assigned managed identity.")]
[CommandExample("Creates a new managed identity", "topaz identity create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
public class CreateManagedIdentityCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<CreateManagedIdentityCommand.CreateManagedIdentityCommandSettings>
{
    public override int Execute(CommandContext context, CreateManagedIdentityCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateManagedIdentityCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var managedIdentityIdentifier = ManagedIdentityIdentifier.From(settings.Name!);
        var controlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);

        var existingIdentity =
            controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        if (existingIdentity.Resource != null)
        {
            logger.LogError($"The specified managed identity: {settings.Name} already exists.");
            return 1;
        }

        var request = new CreateUpdateManagedIdentityRequest
        {
            Location = settings.Location!,
            Tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]),
            Properties = new CreateUpdateManagedIdentityRequest.ManagedIdentityProperties
            {
                IsolationScope = settings.IsolationScope
            }
        };

        var operation = controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier, request);
        if (operation.Result != OperationResult.Created)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateManagedIdentityCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Managed identity name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) managed identity name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) resource tags")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) isolation scope (None or Regional)")]
        [CommandOption("--isolation-scope")]
        public string? IsolationScope { get; set; }
    }
}
