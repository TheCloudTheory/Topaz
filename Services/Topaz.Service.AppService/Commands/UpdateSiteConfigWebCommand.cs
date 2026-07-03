using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice site config update-web", "app-service", "Updates the web configuration of a Web App (Microsoft.Web/sites/config/web). Only supplied fields are changed.")]
[CommandExample("Set always-on and Linux runtime", "topaz appservice site config update-web \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-site\" \\\n    --always-on true \\\n    --linux-fx-version \"DOTNETCORE|8.0\"")]
public sealed class UpdateSiteConfigWebCommand(HttpClient httpClient)
    : TopazHttpCommand<UpdateSiteConfigWebCommand.UpdateSiteConfigWebCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateSiteConfigWebCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Web/sites/{settings.Name}/config/web";

        var properties = new Dictionary<string, object?>();
        if (settings.LinuxFxVersion != null) properties["linuxFxVersion"] = settings.LinuxFxVersion;
        if (settings.NetFrameworkVersion != null) properties["netFrameworkVersion"] = settings.NetFrameworkVersion;
        if (settings.AlwaysOn.HasValue) properties["alwaysOn"] = settings.AlwaysOn.Value;
        if (settings.FtpsState != null) properties["ftpsState"] = settings.FtpsState;
        if (settings.MinTlsVersion != null) properties["minTlsVersion"] = settings.MinTlsVersion;

        var (success, body) = await PutAsync(url, new { properties });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateSiteConfigWebCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("App Service Site name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateSiteConfigWebCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) App Service Site name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("Linux runtime stack (e.g. DOTNETCORE|8.0).", required: false)]
        [CommandOption("--linux-fx-version")]
        public string? LinuxFxVersion { get; set; }

        [CommandOptionDefinition(".NET Framework version (e.g. v8.0).", required: false)]
        [CommandOption("--net-framework-version")]
        public string? NetFrameworkVersion { get; set; }

        [CommandOptionDefinition("Enable Always On.", required: false)]
        [CommandOption("--always-on")]
        public bool? AlwaysOn { get; set; }

        [CommandOptionDefinition("FTPS state (AllAllowed, FtpsOnly, Disabled).", required: false)]
        [CommandOption("--ftps-state")]
        public string? FtpsState { get; set; }

        [CommandOptionDefinition("Minimum TLS version (e.g. 1.2).", required: false)]
        [CommandOption("--min-tls-version")]
        public string? MinTlsVersion { get; set; }
    }
}
