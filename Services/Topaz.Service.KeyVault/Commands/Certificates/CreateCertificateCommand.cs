using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate create", "key-vault", "Creates a self-signed certificate in an Azure Key Vault.")]
[CommandExample("Create a certificate", "topaz keyvault certificate create --vault-name \"kvlocal\" --name \"my-cert\" --subject \"CN=my-cert\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class CreateCertificateCommand(HttpClient httpClient) : TopazHttpCommand<CreateCertificateCommand.CreateCertificateCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, CreateCertificateCommandSettings settings)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/certificates/{settings.Name}/create?api-version=7.4";
        var (success, body) = await PostAsync(url, new
        {
            policy = new
            {
                issuer = new { name = "Self" },
                x509Props = new { subject = settings.Subject ?? $"CN={settings.Name}", validityMonths = settings.ValidityMonths ?? 12 },
                keyProps = new { keySize = settings.KeySize ?? 2048 }
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
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
