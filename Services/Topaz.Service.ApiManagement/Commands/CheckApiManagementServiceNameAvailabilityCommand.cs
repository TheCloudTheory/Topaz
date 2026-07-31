using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands;

[UsedImplicitly]
[CommandDefinition("apim check-name", "api-management", "Checks whether an API Management service name is available.")]
[CommandExample("Check API Management service name availability",
    "topaz apim check-name --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-apim\"")]
internal sealed class CheckApiManagementServiceNameAvailabilityCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CheckApiManagementServiceNameAvailabilityCommand.CheckApiManagementServiceNameAvailabilityCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckApiManagementServiceNameAvailabilityCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.ApiManagement/checkNameAvailability";
        var (success, body) = await PostAsync(url, new
        {
            name = settings.Name,
            type = "Microsoft.ApiManagement/service"
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CheckApiManagementServiceNameAvailabilityCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("API Management service name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckApiManagementServiceNameAvailabilityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name to check")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
