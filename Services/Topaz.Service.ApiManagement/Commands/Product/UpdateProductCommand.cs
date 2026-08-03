using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Commands.Product;

[UsedImplicitly]
[CommandDefinition("apim product update", "api-management", "Updates a product in an Azure API Management service.")]
[CommandExample("Updates a product display name",
    "topaz apim product update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --product-id \"my-product\" \\\n    --display-name \"Updated Product\" \\\n    --resource-group \"rg-local\"")]
internal sealed class UpdateProductCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateProductCommand.UpdateProductCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateProductCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/products/{settings.ProductId}";

        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(new
            {
                displayName = settings.DisplayName,
                description = settings.Description,
                terms = settings.Terms,
                subscriptionRequired = settings.SubscriptionRequired,
                approvalNeeded = settings.ApprovalNeeded,
                state = settings.State
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

    protected override ValidationResult Validate(CommandContext context, UpdateProductCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.ProductId))
            return ValidationResult.Error("Product ID can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateProductCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) product identifier")]
        [CommandOption("--product-id")]
        public string? ProductId { get; set; }

        [CommandOptionDefinition("(Optional) new display name")]
        [CommandOption("--display-name")]
        public string? DisplayName { get; set; }

        [CommandOptionDefinition("(Optional) description")]
        [CommandOption("--description")]
        public string? Description { get; set; }

        [CommandOptionDefinition("(Optional) terms of use")]
        [CommandOption("--terms")]
        public string? Terms { get; set; }

        [CommandOptionDefinition("(Optional) whether a subscription is required to access the product")]
        [CommandOption("--subscription-required")]
        public bool? SubscriptionRequired { get; set; }

        [CommandOptionDefinition("(Optional) whether approval is needed to subscribe")]
        [CommandOption("--approval-needed")]
        public bool? ApprovalNeeded { get; set; }

        [CommandOptionDefinition("(Optional) product state (notPublished or published)")]
        [CommandOption("--state")]
        public string? State { get; set; }

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
