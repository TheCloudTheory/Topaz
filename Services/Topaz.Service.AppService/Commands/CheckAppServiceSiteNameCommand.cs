using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice site check-name", "app-service", "Checks if the provided App Service site name is globally available.")]
[CommandExample("Check App Service site name", "topaz appservice site check-name \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --name \"my-webapp\"")]
public sealed class CheckAppServiceSiteNameCommand(HttpClient httpClient)
    : TopazHttpCommand<CheckAppServiceSiteNameCommand.CheckAppServiceSiteNameCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckAppServiceSiteNameCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Web/checknameavailability";
        var (success, body) = await PostAsync(url, new { name = settings.Name, type = "Microsoft.Web/sites" });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CheckAppServiceSiteNameCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return string.IsNullOrEmpty(settings.Name) ? ValidationResult.Error("App Service site name can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckAppServiceSiteNameCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) App Service site name to check.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
