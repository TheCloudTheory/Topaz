using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Secrets;

[UsedImplicitly]
[CommandDefinition("keyvault secret restore", "key-vault", "Restores a secret into an Azure Key Vault from a backup blob.")]
[CommandExample("Restore a secret", "topaz keyvault secret restore --vault-name \"kvlocal\" --backup-value \"<encoded blob>\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class RestoreSecretCommand(ITopazLogger logger) : Command<RestoreSecretCommand.RestoreSecretCommandSettings>
{
    public override int Execute(CommandContext context, RestoreSecretCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(RestoreSecretCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultDataPlane(logger, new KeyVaultResourceProvider(logger));

        var body = JsonSerializer.Serialize(new RestoreSecretRequest { Value = settings.BackupValue }, GlobalSettings.JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var operation = dataPlane.RestoreSecretBackup(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!);

        if (operation.Result == OperationResult.Failed || operation.Result == OperationResult.NotFound)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, RestoreSecretCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.BackupValue))
            return ValidationResult.Error("Backup value can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RestoreSecretCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Base64url-encoded backup blob produced by the backup command.", required: true)]
        [CommandOption("--backup-value")]
        public string? BackupValue { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
