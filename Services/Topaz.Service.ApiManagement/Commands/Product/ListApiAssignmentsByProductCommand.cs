using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands.Product;

[UsedImplicitly]
[CommandDefinition("apim product list-apis", "api-management", "Lists APIs assigned to a product in an Azure API Management service.")]
[CommandExample("Lists APIs assigned to a product",
    "topaz apim product list-apis --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --product-id \"my-product\" \\\n    --resource-group \"rg-local\"")]
internal sealed class ListApiAssignmentsByProductCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListApiAssignmentsByProductCommand.ListApiAssignmentsByProductCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListApiAssignmentsByProductCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/products/{settings.ProductId}/apis";
        var (success, body) = await GetAsync(url, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListApiAssignmentsByProductCommandSettings settings)
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
    public sealed class ListApiAssignmentsByProductCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) product identifier")]
        [CommandOption("--product-id")]
        public string? ProductId { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
