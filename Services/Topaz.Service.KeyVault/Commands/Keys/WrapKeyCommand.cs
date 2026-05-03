using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key wrap", "key-vault", "Wraps (encrypts) a key using a Key Vault key (RSA keys only: RSA1_5, RSA-OAEP, RSA-OAEP-256).")]
[CommandExample("Wrap with RSA-OAEP-256",
    "topaz keyvault key wrap --vault-name \"kvlocal\" --name \"my-key\" --version \"abc123\" --algorithm \"RSA-OAEP-256\" --value \"aGVsbG8=\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class WrapKeyCommand(ITopazLogger logger) : Command<WrapKeyCommand.WrapKeyCommandSettings>
{
    public override int Execute(CommandContext context, WrapKeyCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultKeysDataPlane(logger, new KeyVaultResourceProvider(logger));

        var request = new KeyOperationRequest
        {
            Algorithm = settings.Algorithm,
            Value = settings.Value
        };

        var operation = dataPlane.WrapKey(subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!, settings.Version!, request);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        if (operation.Result == OperationResult.Failed)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, WrapKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.Version))
            return ValidationResult.Error("Key version can't be null. Use 'topaz keyvault key get' to retrieve the version.");
        if (string.IsNullOrEmpty(settings.Algorithm))
            return ValidationResult.Error("Algorithm can't be null. Supported: RSA1_5, RSA-OAEP, RSA-OAEP-256.");
        if (string.IsNullOrEmpty(settings.Value))
            return ValidationResult.Error("Value can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class WrapKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Key version.", required: true)]
        [CommandOption("--version")]
        public string? Version { get; set; }

        [CommandOptionDefinition("(Required) Wrap algorithm. Supported: RSA1_5, RSA-OAEP, RSA-OAEP-256.", required: true)]
        [CommandOption("-a|--algorithm")]
        public string? Algorithm { get; set; }

        [CommandOptionDefinition("(Required) Base64url-encoded key material to wrap.", required: true)]
        [CommandOption("--value")]
        public string? Value { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
