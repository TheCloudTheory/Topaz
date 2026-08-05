using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Commands.Policy;

[UsedImplicitly]
[CommandDefinition("apim policy create", "api-management", "Creates or updates a policy in an Azure API Management service.")]
[CommandExample("Creates a policy in an API Management service",
    "topaz apim policy create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --policy-id \"policy\" \\\n    --value \"<policies><inbound /></policies>\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateOrUpdatePolicyCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdatePolicyCommand.CreateOrUpdatePolicyCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdatePolicyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/policies/{settings.PolicyId}";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new
            {
                properties = new
                {
                    format = settings.Format,
                    value = settings.Value
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

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdatePolicyCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.PolicyId))
            return ValidationResult.Error("Policy ID can't be null.");
        if (string.IsNullOrEmpty(settings.Value))
            return ValidationResult.Error("Policy value can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdatePolicyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) policy identifier")]
        [CommandOption("--policy-id")]
        public string? PolicyId { get; set; }

        [CommandOptionDefinition("(Required) policy content")]
        [CommandOption("--value")]
        public string? Value { get; set; }

        [CommandOptionDefinition("(Optional) policy content format (default: xml)")]
        [CommandOption("--format")]
        public string? Format { get; set; } = "xml";

        [CommandOptionDefinition("(Optional) ETag for optimistic concurrency")]
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
