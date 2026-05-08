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
[CommandDefinition("keyvault certificate update", "key-vault", "Updates the attributes of a certificate in an Azure Key Vault.")]
[CommandExample("Disable a certificate", "topaz keyvault certificate update --vault-name \"kvlocal\" --name \"my-cert\" --version \"<version-guid>\" --enabled false --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class UpdateCertificateCommand(ITopazLogger logger) : Command<UpdateCertificateCommand.UpdateCertificateCommandSettings>
{
    public override int Execute(CommandContext context, UpdateCertificateCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var request = new UpdateCertificateRequest
        {
            Attributes = settings.Enabled.HasValue
                ? new UpdateCertificateRequest.UpdateCertificateAttributes { Enabled = settings.Enabled }
                : null
        };

        var requestJson = JsonSerializer.Serialize(request, GlobalSettings.JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));

        var operation = dataPlane.UpdateCertificate(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!, settings.Version!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateCertificateCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Certificate name can't be null.");
        if (string.IsNullOrEmpty(settings.Version))
            return ValidationResult.Error("Certificate version can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateCertificateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Certificate name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Certificate version.", required: true)]
        [CommandOption("--version")]
        public string? Version { get; set; }

        [CommandOptionDefinition("(Optional) Enable or disable the certificate.")]
        [CommandOption("--enabled")]
        public bool? Enabled { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
