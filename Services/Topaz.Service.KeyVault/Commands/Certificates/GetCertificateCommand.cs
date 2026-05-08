using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate get", "key-vault", "Gets a certificate from an Azure Key Vault.")]
[CommandExample("Get a certificate", "topaz keyvault certificate get --vault-name \"kvlocal\" --name \"my-cert\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class GetCertificateCommand(ITopazLogger logger) : Command<GetCertificateCommand.GetCertificateCommandSettings>
{
    public override int Execute(CommandContext context, GetCertificateCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var operation = dataPlane.GetCertificate(subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!, settings.Version);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetCertificateCommandSettings settings)
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
    public sealed class GetCertificateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Certificate name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Optional) Certificate version. Omit to get the latest.")]
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
