using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key list", "key-vault", "Lists all keys in an Azure Key Vault.")]
[CommandExample("List all keys in a vault",
    "topaz keyvault key list --vault-name \"kvlocal\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ListKeysCommand(ITopazLogger logger) : Command<ListKeysCommand.ListKeysCommandSettings>
{
    public override int Execute(CommandContext context, ListKeysCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(ListKeysCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultDataPlane(logger, new KeyVaultResourceProvider(logger));

        var operation = dataPlane.GetKeys(subscriptionIdentifier, resourceGroupIdentifier, settings.VaultName!);

        logger.LogInformation(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListKeysCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListKeysCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
