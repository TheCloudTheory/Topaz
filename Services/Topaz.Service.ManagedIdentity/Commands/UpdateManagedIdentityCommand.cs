using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity update", "managed-identity", "Updates a user-assigned managed identity.")]
[CommandExample("Updates a managed identity with tags", "topaz identity update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --resource-group \"rg-local\" \\\n    --tags environment=production team=devops")]
public sealed class UpdateManagedIdentityCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<UpdateManagedIdentityCommand.UpdateManagedIdentityCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateManagedIdentityCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{settings.Name}";
        var (success, body) = await PatchAsync(url, new
        {
            tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1])
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateManagedIdentityCommandSettings settings)
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
    public sealed class UpdateManagedIdentityCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Optional) resource tags")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) isolation scope (None or Regional)")]
        [CommandOption("--isolation-scope")]
        public string? IsolationScope { get; set; }
    }
}
