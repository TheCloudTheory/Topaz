using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate cancel-operation", "key-vault", "Cancels the pending creation operation for a certificate in an Azure Key Vault.")]
[CommandExample("Cancel a certificate pending operation", "topaz keyvault certificate cancel-operation --vault-name \"kvlocal\" --name \"my-cert\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class CancelCertificateOperationCommand(ITopazLogger logger) : Command<CancelCertificateOperationCommand.CancelCertificateOperationCommandSettings>
{
    public override int Execute(CommandContext context, CancelCertificateOperationCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var result = dataPlane.UpdateCertificateOperation(
            new System.IO.MemoryStream(
                System.Text.Encoding.UTF8.GetBytes("{\"cancellation_requested\":true}")),
            subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!);

        if (result.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({result.Code}) {result.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(result.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CancelCertificateOperationCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Certificate name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CancelCertificateOperationCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Certificate name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
