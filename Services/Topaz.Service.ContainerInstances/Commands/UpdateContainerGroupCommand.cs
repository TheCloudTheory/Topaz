using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerInstances.Commands;

[UsedImplicitly]
[CommandDefinition("containerinstances group update", "container-instances", "Updates properties of an existing Container Group.")]
[CommandExample("Update a container group", "topaz containerinstances group update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-container-group\" \\\n    --restart-policy \"OnFailure\" \\\n    --priority \"Spot\"")]
public sealed class UpdateContainerGroupCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateContainerGroupCommand.UpdateContainerGroupCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateContainerGroupCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{settings.Name}";
        var body = new
        {
            properties = new
            {
                osType = settings.OsType,
                restartPolicy = settings.RestartPolicy,
                sku = settings.Sku,
                priority = settings.Priority
            }
        };
        var (success, responseBody) = await PutAsync(url, body, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateContainerGroupCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Container group name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateContainerGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Container group name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Optional) OS type: Linux or Windows.", required: false)]
        [CommandOption("--os-type")]
        public string? OsType { get; set; }

        [CommandOptionDefinition("(Optional) Restart policy: Always, OnFailure, or Never.", required: false)]
        [CommandOption("--restart-policy")]
        public string? RestartPolicy { get; set; }

        [CommandOptionDefinition("(Optional) SKU: Standard or Dedicated.", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Optional) Priority: Regular or Spot.", required: false)]
        [CommandOption("--priority")]
        public string? Priority { get; set; }
    }
}
