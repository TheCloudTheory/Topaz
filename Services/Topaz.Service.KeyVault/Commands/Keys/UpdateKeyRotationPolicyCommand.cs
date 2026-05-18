using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key rotation-policy update", "key-vault", "Updates the rotation policy for a key in an Azure Key Vault.")]
[CommandExample("Set a 2-year expiry on a key rotation policy",
    "topaz keyvault key rotation-policy update --vault-name \"kvlocal\" --name \"my-key\" --expires-in \"P2Y\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class UpdateKeyRotationPolicyCommand(HttpClient httpClient) : TopazHttpCommand<UpdateKeyRotationPolicyCommand.UpdateKeyRotationPolicyCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, UpdateKeyRotationPolicyCommandSettings settings)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/{settings.Name}/rotationpolicy?api-version=7.4";
        var (success, body) = await PutAsync(url, new { attributes = new { expiryTime = settings.ExpiresIn }, lifetimeActions = Array.Empty<object>() });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateKeyRotationPolicyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateKeyRotationPolicyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("The expiry time as an ISO 8601 duration (e.g. P2Y). If omitted, the expiry is cleared.")]
        [CommandOption("--expires-in")]
        public string? ExpiresIn { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
