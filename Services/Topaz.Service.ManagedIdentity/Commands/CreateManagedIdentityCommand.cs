using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagedIdentity.Commands;

[UsedImplicitly]
[CommandDefinition("identity create", "managed-identity", "Creates a new user-assigned managed identity.")]
[CommandExample("Creates a new managed identity", "topaz identity create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"myIdentity\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
public class CreateManagedIdentityCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateManagedIdentityCommand.CreateManagedIdentityCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateManagedIdentityCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{settings.Name}";

        // Fail early if the identity already exists
        var (exists, _) = await GetAsync(url);
        if (exists)
        {
            await Console.Error.WriteLineAsync($"Error: Managed identity '{settings.Name}' already exists.");
            return 1;
        }

        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            tags = settings.Tags?.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1])
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateManagedIdentityCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Managed identity name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Location can't be null.");
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
    public sealed class CreateManagedIdentityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) managed identity name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) resource tags")]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) isolation scope (None or Regional)")]
        [CommandOption("--isolation-scope")]
        public string? IsolationScope { get; set; }
    }
}
