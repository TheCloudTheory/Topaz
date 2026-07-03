using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate update", "key-vault", "Updates the attributes of a certificate in an Azure Key Vault.")]
[CommandExample("Disable a certificate", "topaz keyvault certificate update --vault-name \"kvlocal\" --name \"my-cert\" --version \"<version-guid>\" --enabled false --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class UpdateCertificateCommand(HttpClient httpClient) : TopazHttpCommand<UpdateCertificateCommand.UpdateCertificateCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateCertificateCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/certificates/{settings.Name}/{settings.Version ?? ""}?api-version=7.4";
        var body = new { attributes = settings.Enabled.HasValue ? new { enabled = settings.Enabled } : (object?)null };
        var (success, response) = await PatchAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(response);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateCertificateCommandSettings settings)
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
