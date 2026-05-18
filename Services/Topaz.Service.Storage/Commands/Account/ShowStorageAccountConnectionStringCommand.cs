using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account show-connection-string", "azure-storage/account", "Shows the connection string for a storage account.")]
[CommandExample("Show connection string", "topaz storage account show-connection-string \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\"")]
public sealed class ShowStorageAccountConnectionStringCommand(HttpClient httpClient) : TopazHttpCommand<ShowStorageAccountConnectionStringCommand.ShowStorageAccountConnectionStringCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
    {
        var keysUrl = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.Name}/listKeys";
        var (success, body) = await PostAsync(keysUrl, new { });
        if (!success) return 1;
        var keysResponse = JsonSerializer.Deserialize<JsonElement>(body);
        var key = keysResponse.GetProperty("keys")[0].GetProperty("value").GetString();
        var cs = $"DefaultEndpointsProtocol=https;AccountName={settings.Name};AccountKey={key};" +
                 $"BlobEndpoint={BlobDataPlaneUrl(settings.Name!)};" +
                 $"QueueEndpoint={QueueDataPlaneUrl(settings.Name!)};" +
                 $"TableEndpoint={TableDataPlaneUrl(settings.Name!)}";
        AnsiConsole.WriteLine(cs);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Service Bus namespace resource group can't be null.");
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
    public sealed class ShowStorageAccountConnectionStringCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}