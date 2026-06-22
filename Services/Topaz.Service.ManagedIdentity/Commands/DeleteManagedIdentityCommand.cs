using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity delete", "managed-identity", "Deletes a user-assigned managed identity.")]
[CommandExample("Deletes a managed identity", "topaz identity delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --resource-group \"rg-local\"")]
public sealed class DeleteManagedIdentityCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<DeleteManagedIdentityCommand.DeleteManagedIdentityCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteManagedIdentityCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Managed identity '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteManagedIdentityCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Managed identity name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }
        
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
    public sealed class DeleteManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) managed identity name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;
    }
}
