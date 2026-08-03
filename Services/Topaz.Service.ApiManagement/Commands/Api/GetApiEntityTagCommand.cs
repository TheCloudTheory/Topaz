using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands.Api;

[UsedImplicitly]
[CommandDefinition("apim api get-entity-tag", "api-management", "Gets the entity tag (ETag) for an API in an Azure API Management service.")]
[CommandExample("Gets the ETag for an API",
    "topaz apim api get-entity-tag --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --api-id \"my-api\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetApiEntityTagCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetApiEntityTagCommand.GetApiEntityTagCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GetApiEntityTagCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/apis/{settings.ApiId}";

        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}");
            return 1;
        }
        var etag = response.Headers.ETag?.Tag ?? "(no ETag)";
        AnsiConsole.WriteLine(etag);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, GetApiEntityTagCommandSettings settings)
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
    public sealed class GetApiEntityTagCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) API identifier")]
        [CommandOption("--api-id")]
        public string? ApiId { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
