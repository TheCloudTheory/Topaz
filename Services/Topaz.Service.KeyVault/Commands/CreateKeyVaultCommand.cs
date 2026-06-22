using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
[CommandDefinition("keyvault create",  "key-vault", "Creates a new Azure Key Vault.")]
[CommandExample("Creates a new Key Vault", "topaz keyvault create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"kvlocal\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
public class CreateKeyVaultCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateKeyVaultCommand.CreateKeyVaultCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateKeyVaultCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.KeyVault/vaults/{settings.Name}";

        // Check whether the vault already exists; if so, fail gracefully.
        var getResponse = await HttpClient.GetAsync(url);
        if (getResponse.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"The specified vault: {settings.Name} already exists.");
            return 1;
        }

        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                sku = new { name = "standard", family = "A" },
                tenantId = "00000000-0000-0000-0000-000000000000",
                accessPolicies = Array.Empty<object>()
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateKeyVaultCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Resource group location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateKeyVaultCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) vault name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Key Vault location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
