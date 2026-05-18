using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr check-name", "container-registry", "Checks whether a container registry name is available.")]
[CommandExample("Check registry name availability", "topaz acr check-name \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --name \"myregistry\"")]
public sealed class CheckContainerRegistryNameCommand(HttpClient httpClient)
    : TopazHttpCommand<CheckContainerRegistryNameCommand.CheckNameCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, CheckNameCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.ContainerRegistry/checkNameAvailability";
        var (success, body) = await PostAsync(url, new { name = settings.Name, type = "Microsoft.ContainerRegistry/registries" });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckNameCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckNameCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Registry name to check.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
