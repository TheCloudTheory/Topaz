using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Commands.Api;

[UsedImplicitly]
[CommandDefinition("apim api update", "api-management", "Updates an API in an Azure API Management service.")]
[CommandExample("Updates an API display name",
    "topaz apim api update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --api-id \"my-api\" \\\n    --display-name \"Updated API\" \\\n    --resource-group \"rg-local\"")]
internal sealed class UpdateApiCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateApiCommand.UpdateApiCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateApiCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/apis/{settings.ApiId}";

        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(new
            {
                properties = new
                {
                    displayName = settings.DisplayName,
                    path = settings.Path,
                    serviceUrl = settings.ServiceUrl,
                    description = settings.Description
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

    protected override ValidationResult Validate(CommandContext context, UpdateApiCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.ApiId))
            return ValidationResult.Error("API ID can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateApiCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) API identifier")]
        [CommandOption("--api-id")]
        public string? ApiId { get; set; }

        [CommandOptionDefinition("(Optional) new display name")]
        [CommandOption("--display-name")]
        public string? DisplayName { get; set; }

        [CommandOptionDefinition("(Optional) relative URL path")]
        [CommandOption("--path")]
        public string? Path { get; set; }

        [CommandOptionDefinition("(Optional) backend service URL")]
        [CommandOption("--service-url")]
        public string? ServiceUrl { get; set; }

        [CommandOptionDefinition("(Optional) description")]
        [CommandOption("--description")]
        public string? Description { get; set; }

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
