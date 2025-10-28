using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
[CommandDefinition("keyvault create",  "key-vault", "Creates a new Azure Key Vault.")]
[CommandExample("Creates a new Key Vault", "topaz keyvault create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"kvlocal\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
public class CreateKeyVaultCommand(ITopazLogger logger) : Command<CreateKeyVaultCommand.CreateKeyVaultCommandSettings>
{
    public override int Execute(CommandContext context, CreateKeyVaultCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateKeyVaultCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = new KeyVaultControlPlane(new KeyVaultResourceProvider(logger), new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger));
        var existingKeyVault = controlPlane.CheckName(subscriptionIdentifier, settings.Name!, null);

        if (!existingKeyVault.response.NameAvailable)
        {
            logger.LogError($"The specified vault: {settings.Name} already exists.");
            return 1;
        }
        
        var operation = controlPlane.Create(subscriptionIdentifier, resourceGroupIdentifier, settings.Location!, settings.Name!);
        if (operation.Result != OperationResult.Created)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateKeyVaultCommandSettings settings)
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
    public sealed class CreateKeyVaultCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) vault name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Key Vault location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
