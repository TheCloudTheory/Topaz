using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Commands.Backend;

[UsedImplicitly]
[CommandDefinition("apim backend reconnect", "api-management", "Notifies API Management to create a new connection to the backend after the specified timeout.")]
[CommandExample("Triggers reconnect for a backend",
    "topaz apim backend reconnect --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --backend-id \"my-backend\" \\\n    --resource-group \"rg-local\"")]
internal sealed class ReconnectBackendCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ReconnectBackendCommand.ReconnectBackendCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ReconnectBackendCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/backends/{settings.BackendId}/reconnect";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                properties = new
                {
                    after = settings.After
                }
            }, options: GlobalSettings.JsonOptions)
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
            return 1;
        }
        AnsiConsole.WriteLine($"Backend '{settings.BackendId}' reconnect accepted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ReconnectBackendCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.BackendId))
            return ValidationResult.Error("Backend ID can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ReconnectBackendCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) backend identifier")]
        [CommandOption("--backend-id")]
        public string? BackendId { get; set; }

        [CommandOptionDefinition("(Optional) duration after which reconnect is initiated (ISO 8601 duration, e.g. PT3S)")]
        [CommandOption("--after")]
        public string? After { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
