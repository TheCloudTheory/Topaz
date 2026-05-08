using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models.Requests.Certificates;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate import", "key-vault", "Imports a PFX/PKCS#12 certificate into an Azure Key Vault.")]
[CommandExample("Import a certificate", "topaz keyvault certificate import --vault-name \"kvlocal\" --name \"my-cert\" --value \"<base64-pfx>\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ImportCertificateCommand(ITopazLogger logger) : Command<ImportCertificateCommand.ImportCertificateCommandSettings>
{
    public override int Execute(CommandContext context, ImportCertificateCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var request = new ImportCertificateRequest
        {
            Value = settings.Value!,
            Password = settings.Password
        };

        var requestJson = JsonSerializer.Serialize(request, GlobalSettings.JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));

        var operation = dataPlane.ImportCertificate(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!);

        if (operation.Result == OperationResult.Failed)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ImportCertificateCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Certificate name can't be null.");
        if (string.IsNullOrEmpty(settings.Value))
            return ValidationResult.Error("Certificate value (base64 PFX) can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ImportCertificateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Certificate name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Base64-encoded PFX/PKCS#12 certificate bytes.", required: true)]
        [CommandOption("--value")]
        public string? Value { get; set; }

        [CommandOptionDefinition("(Optional) Password for the PFX file.")]
        [CommandOption("--password")]
        public string? Password { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
