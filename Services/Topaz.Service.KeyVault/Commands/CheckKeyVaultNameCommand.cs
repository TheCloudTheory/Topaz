using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
[CommandDefinition("keyvault check-name",  "key-vault", "Checks if the provided Key Vault name is available.")]
[CommandExample("Check Key Vault name", "topaz keyvault check-name \\\n    --name \"sb-namespace\" \\\n    --resource-group \"rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public class CheckKeyVaultNameCommand(HttpClient httpClient) : TopazHttpCommand<CheckKeyVaultNameCommand.CheckKeyVaultNameCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckKeyVaultNameCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.KeyVault/checkNameAvailability";
        var (success, body) = await PostAsync(url, new { name = settings.Name, type = "Microsoft.KeyVault/vaults" });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CheckKeyVaultNameCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }
        
        return string.IsNullOrEmpty(settings.Name) ? ValidationResult.Error("Key vault name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CheckKeyVaultNameCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.")]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        
        [CommandOptionDefinition("Type of Key Vault to create.")]
        [CommandOption("--resource-type")] public string? ResourceType { get; set; }
        
        [CommandOptionDefinition("(Required) Key Vault subscription ID.")]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
