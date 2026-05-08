using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests.Certificates;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate create", "key-vault", "Creates a self-signed certificate in an Azure Key Vault.")]
[CommandExample("Create a certificate", "topaz keyvault certificate create --vault-name \"kvlocal\" --name \"my-cert\" --subject \"CN=my-cert\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class CreateCertificateCommand(ITopazLogger logger) : Command<CreateCertificateCommand.CreateCertificateCommandSettings>
{
    public override int Execute(CommandContext context, CreateCertificateCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultCertificatesDataPlane(logger, new KeyVaultResourceProvider(logger));

        var request = new CreateCertificateRequest
        {
            Policy = new CertificatePolicy
            {
                Issuer = new CertificatePolicy.IssuerParameters { Name = "Self" },
                X509Props = new CertificatePolicy.X509CertificateProperties
                {
                    Subject = settings.Subject ?? $"CN={settings.Name}",
                    ValidityMonths = settings.ValidityMonths ?? 12
                },
                KeyProps = new CertificatePolicy.KeyProperties
                {
                    KeySize = settings.KeySize ?? 2048
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, GlobalSettings.JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));

        var operation = dataPlane.CreateCertificate(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!);

        if (operation.Result == OperationResult.Failed)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.Bundle.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateCertificateCommandSettings settings)
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
    public sealed class CreateCertificateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Certificate name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Optional) X.509 subject (e.g. CN=my-cert). Defaults to CN=<name>.")]
        [CommandOption("--subject")]
        public string? Subject { get; set; }

        [CommandOptionDefinition("(Optional) Validity in months. Defaults to 12.")]
        [CommandOption("--validity-months")]
        public int? ValidityMonths { get; set; }

        [CommandOptionDefinition("(Optional) RSA key size in bits. Defaults to 2048.")]
        [CommandOption("--key-size")]
        public int? KeySize { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
