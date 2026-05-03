using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key get-attestation", "key-vault", "Gets a key and its attestation information from an Azure Key Vault. For software-backed keys the attestation field is null, matching real Azure behaviour.")]
[CommandExample("Get attestation for the latest version of a key",
    "topaz keyvault key get-attestation --vault-name \"kvlocal\" --name \"my-key\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
[CommandExample("Get attestation for a specific version of a key",
    "topaz keyvault key get-attestation --vault-name \"kvlocal\" --name \"my-key\" --version \"abc123\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class GetKeyAttestationCommand(ITopazLogger logger) : Command<GetKeyAttestationCommand.GetKeyAttestationCommandSettings>
{
    public override int Execute(CommandContext context, GetKeyAttestationCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultKeysDataPlane(logger, new KeyVaultResourceProvider(logger));

        var operation = dataPlane.GetKey(subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!, settings.Version);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"Key '{settings.Name}' not found.");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetKeyAttestationCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetKeyAttestationCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("Key version (omit to retrieve the latest version).")]
        [CommandOption("--version")]
        public string? Version { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
