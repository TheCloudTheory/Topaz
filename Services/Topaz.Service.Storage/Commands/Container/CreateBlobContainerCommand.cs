using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container create", "azure-storage/container", "Creates a new blob container in a storage account.")]
[CommandExample("Create a container", "topaz storage container create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"mycontainer\"")]
public sealed class CreateBlobContainerCommand(HttpClient httpClient) : TopazHttpCommand<CreateBlobContainerCommand.CreateBlobContainerCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateBlobContainerCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine("Creating blob container...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.AccountName}/blobServices/default/containers/{settings.Name}";
        var (success, body) = await PutAsync(url, new { properties = new { publicAccess = (string?)null } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Container name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Storage account subscription ID can't be null.");
        }

        return string.IsNullOrEmpty(settings.AccountName) ? 
            ValidationResult.Error("Storage account name can't be null.") 
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateBlobContainerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
