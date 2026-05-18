using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity list", "managed-identity", "Lists user-assigned managed identities.")]
[CommandExample("Lists managed identities by resource group", "topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
[CommandExample("Lists managed identities by subscription", "topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae")]
public sealed class ListManagedIdentityCommand(HttpClient httpClient) : TopazHttpCommand<ListManagedIdentityCommand.ListManagedIdentityCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListManagedIdentityCommandSettings settings)
    {
        string url;
        if (!string.IsNullOrEmpty(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.ManagedIdentity/userAssignedIdentities";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListManagedIdentityCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Optional) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
    }
}
