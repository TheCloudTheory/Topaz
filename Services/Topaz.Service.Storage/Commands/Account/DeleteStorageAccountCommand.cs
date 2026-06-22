using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account delete", "azure-storage/account", "Deletes an Azure Storage account.")]
[CommandExample("Delete a storage account", "topaz storage account delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\"")]
public class DeleteStorageAccountCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<DeleteStorageAccountCommand.DeleteStorageAccountCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        AnsiConsole.WriteLine("Deleting storage account...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.Name}";
        var success = await DeleteAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine($"Storage account '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteStorageAccountCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
