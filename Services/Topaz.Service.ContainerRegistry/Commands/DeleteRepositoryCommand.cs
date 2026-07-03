using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr repository delete", "container-registry", "Deletes a repository from an Azure Container Registry.")]
[CommandExample("Delete a repository", "topaz acr repository delete \\\n+    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n+    --resource-group \"my-rg\" \\\n+    --registry \"myregistry\" \\\n+    --name \"sample-repository\"")]
public sealed class DeleteRepositoryCommand(HttpClient httpClient)
    : TopazHttpCommand<DeleteRepositoryCommand.DeleteRepositoryCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteRepositoryCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"https://{settings.Registry}.azurecr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}/acr/v1/{settings.Name}";
        var success = await DeleteAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine($"Repository '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteRepositoryCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Repository name can't be null.");

        if (string.IsNullOrEmpty(settings.Registry))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteRepositoryCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Repository name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Registry name.")]
        [CommandOption("-r|--registry")]
        public string? Registry { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}