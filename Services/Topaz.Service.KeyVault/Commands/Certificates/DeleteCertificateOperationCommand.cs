using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Certificates;

[UsedImplicitly]
[CommandDefinition("keyvault certificate delete-operation", "key-vault", "Deletes the pending creation operation for a certificate in an Azure Key Vault.")]
[CommandExample("Delete a certificate pending operation", "topaz keyvault certificate delete-operation --vault-name \"kvlocal\" --name \"my-cert\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class DeleteCertificateOperationCommand(HttpClient httpClient) : TopazHttpCommand<DeleteCertificateOperationCommand.DeleteCertificateOperationCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteCertificateOperationCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/certificates/{settings.Name}/pending?api-version=7.4";
        var success = await DeleteAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine($"Certificate operation for '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteCertificateOperationCommandSettings settings)
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
    public sealed class DeleteCertificateOperationCommandSettings : CommandSettings
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
