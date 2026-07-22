using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands.Replicas;

[UsedImplicitly]
[CommandDefinition("appconfig replica delete", "app-configuration", "Deletes a replica from an App Configuration store.")]
[CommandExample("Delete a replica",
    "topaz appconfig replica delete --name \"my-appconfig\" --replica-name \"my-replica\" --resource-group \"rg-local\" --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae")]
internal sealed class DeleteReplicaCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteReplicaCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{settings.Name}/replicas/{settings.ReplicaName}";
        if (!await DeleteAsync(url, cancellationToken)) return 1;
        AnsiConsole.WriteLine($"Replica '{settings.ReplicaName}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Store name can't be null.");
        if (string.IsNullOrEmpty(settings.ReplicaName))
            return ValidationResult.Error("Replica name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Store name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Replica name.", required: true)]
        [CommandOption("--replica-name")]
        public string? ReplicaName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
