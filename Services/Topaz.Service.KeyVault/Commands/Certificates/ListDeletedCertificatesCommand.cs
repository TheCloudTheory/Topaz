using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models.Responses.Certificates;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate list-deleted", "key-vault", "Lists soft-deleted certificates in an Azure Key Vault.")]
[CommandExample("List deleted certificates", "topaz keyvault certificate list-deleted --vault-name \"kvlocal\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ListDeletedCertificatesCommand(ITopazLogger logger) : Command<ListDeletedCertificatesCommand.ListDeletedCertificatesCommandSettings>
{
    public override int Execute(CommandContext context, ListDeletedCertificatesCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var operation = dataPlane.GetDeletedCertificates(subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!);

        AnsiConsole.WriteLine(GetDeletedCertificatesResponse.FromRecords(operation.Resource!, settings.VaultName!).ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListDeletedCertificatesCommandSettings settings)
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
    public sealed class ListDeletedCertificatesCommandSettings : CommandSettings
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
