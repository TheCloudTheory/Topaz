using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate import", "key-vault", "Imports a PFX/PKCS#12 certificate into an Azure Key Vault.")]
[CommandExample("Import a certificate", "topaz keyvault certificate import --vault-name \"kvlocal\" --name \"my-cert\" --value \"<base64-pfx>\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ImportCertificateCommand(HttpClient httpClient) : TopazHttpCommand<ImportCertificateCommand.ImportCertificateCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, ImportCertificateCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/certificates/{settings.Name}/import?api-version=7.4";
        var (success, body) = await PostAsync(url, new { value = settings.Value, pwd = settings.Password });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ImportCertificateCommandSettings settings)
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
