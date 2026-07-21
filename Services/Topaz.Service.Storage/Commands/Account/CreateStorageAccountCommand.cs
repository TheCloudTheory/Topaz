using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Account;

[UsedImplicitly]
[CommandDefinition("storage account create", "azure-storage/account", "Creates a new Azure Storage account.")]
[CommandExample("Create a storage account", "topaz storage account create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\" \\\n    --location \"westeurope\"")]
[CommandExample("Create a storage account with hierarchical namespace (HNS)", "topaz storage account create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"salocal\" \\\n    --location \"westeurope\" \\\n    --enable-hierarchical-namespace")]
public sealed class CreateStorageAccountCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateStorageAccountCommand.CreateStorageAccountCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateStorageAccountCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine("Creating storage account...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.Name}";
        var (success, body) = await PutAsync(url, new { location = settings.Location, kind = "StorageV2", sku = new { name = "Standard_LRS" }, properties = new
        {
            isHnsEnabled = settings.EnableHierarchicalNamespace
        } }, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Storage account location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Storage account subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateStorageAccountCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Location (e.g. westeurope).", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
        
        [CommandOptionDefinition("Enable HNS.", required: false)]
        [CommandOption("--enable-hierarchical-namespace")]
        public bool EnableHierarchicalNamespace { get; set; }
    }
}
