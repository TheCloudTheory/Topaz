using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands.Product;

[UsedImplicitly]
[CommandDefinition("apim product check-api", "api-management", "Checks whether an API is assigned to a product in an Azure API Management service.")]
[CommandExample("Checks if an API is assigned to a product",
    "topaz apim product check-api --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --product-id \"my-product\" \\\n    --api-id \"my-api\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CheckProductApiAssignmentExistCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CheckProductApiAssignmentExistCommand.CheckProductApiAssignmentExistCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckProductApiAssignmentExistCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/products/{settings.ProductId}/apis/{settings.ApiId}";

        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.IsSuccessStatusCode)
        {
            AnsiConsole.WriteLine($"API '{settings.ApiId}' is assigned to product '{settings.ProductId}'.");
            return 0;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            AnsiConsole.WriteLine($"API '{settings.ApiId}' is NOT assigned to product '{settings.ProductId}'.");
            return 1;
        }
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}");
        return 1;
    }

    protected override ValidationResult Validate(CommandContext context, CheckProductApiAssignmentExistCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.ProductId))
            return ValidationResult.Error("Product ID can't be null.");
        if (string.IsNullOrEmpty(settings.ApiId))
            return ValidationResult.Error("API ID can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckProductApiAssignmentExistCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) product identifier")]
        [CommandOption("--product-id")]
        public string? ProductId { get; set; }

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
