using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Commands.Backend;

[UsedImplicitly]
[CommandDefinition("apim backend create", "api-management", "Creates or updates a backend in an Azure API Management service.")]
[CommandExample("Creates a new backend in an API Management service",
    "topaz apim backend create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --backend-id \"my-backend\" \\\n    --url \"https://backend.example.com\" \\\n    --protocol \"http\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateOrUpdateBackendCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateBackendCommand.CreateOrUpdateBackendCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateBackendCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/backends/{settings.BackendId}";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new
            {
                properties = new
                {
                    url = settings.Url,
                    protocol = settings.Protocol,
                    description = settings.Description,
                    title = settings.Title,
                    resourceId = settings.ResourceId,
                    type = settings.Type
                }
            }, options: GlobalSettings.JsonOptions)
        };
        if (!string.IsNullOrEmpty(settings.IfMatch))
            request.Headers.TryAddWithoutValidation("If-Match", settings.IfMatch);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
            return 1;
        }
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateBackendCommandSettings settings)
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
    public sealed class CreateOrUpdateBackendCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) backend identifier")]
        [CommandOption("--backend-id")]
        public string? BackendId { get; set; }

        [CommandOptionDefinition("(Optional) runtime URL of the backend")]
        [CommandOption("--url")]
        public string? Url { get; set; }

        [CommandOptionDefinition("(Optional) backend communication protocol (http or soap)")]
        [CommandOption("--protocol")]
        public string? Protocol { get; set; }

        [CommandOptionDefinition("(Optional) backend description")]
        [CommandOption("--description")]
        public string? Description { get; set; }

        [CommandOptionDefinition("(Optional) backend title")]
        [CommandOption("--title")]
        public string? Title { get; set; }

        [CommandOptionDefinition("(Optional) management URI of the backend in external system")]
        [CommandOption("--resource-id")]
        public string? ResourceId { get; set; }

        [CommandOptionDefinition("(Optional) type of the backend (Single or Pool)")]
        [CommandOption("--type")]
        public string? Type { get; set; }

        [CommandOptionDefinition("(Optional) ETag for conditional update")]
        [CommandOption("--if-match")]
        public string? IfMatch { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
