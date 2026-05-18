using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr repository list", "container-registry", "Lists repositories in an Azure Container Registry.")]
[CommandExample("List repositories in a registry", "topaz acr repository list \\\n+    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n+    --resource-group \"my-rg\" \\\n+    --registry \"myregistry\"")]
public sealed class ListRepositoriesCommand(HttpClient httpClient)
    : TopazHttpCommand<ListRepositoriesCommand.ListRepositoriesCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, ListRepositoriesCommandSettings settings)
    {
        var url = $"https://{settings.Registry}.azurecr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}/acr/v1/_catalog";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListRepositoriesCommandSettings settings)
    {
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
    public sealed class ListRepositoriesCommandSettings : CommandSettings
    {
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