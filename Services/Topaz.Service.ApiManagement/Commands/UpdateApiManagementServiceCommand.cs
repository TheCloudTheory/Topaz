using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands;

[UsedImplicitly]
[CommandDefinition("apim update", "api-management", "Updates an Azure API Management service.")]
[CommandExample("Updates an API Management service SKU",
    "topaz apim update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-apim\" \\\n    --resource-group \"rg-local\" \\\n    --sku-name \"Standard\" \\\n    --sku-capacity 2")]
internal sealed class UpdateApiManagementServiceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateApiManagementServiceCommand.UpdateApiManagementServiceCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateApiManagementServiceCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.Name}";

        var updatePayload = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(settings.SkuName) || settings.SkuCapacity.HasValue)
        {
            updatePayload["sku"] = new
            {
                name = settings.SkuName,
                capacity = settings.SkuCapacity
            };
        }
        if (!string.IsNullOrEmpty(settings.PublisherEmail) || !string.IsNullOrEmpty(settings.PublisherName))
        {
            updatePayload["properties"] = new
            {
                publisherEmail = settings.PublisherEmail,
                publisherName = settings.PublisherName
            };
        }

        if (updatePayload.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: No updates specified.[/]");
            return 0;
        }

        var (success, body) = await PatchAsync(url, updatePayload, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateApiManagementServiceCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("API Management service name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateApiManagementServiceCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Developer, Basic, Standard, Premium)")]
        [CommandOption("--sku-name")]
        public string? SkuName { get; set; }

        [CommandOptionDefinition("(Optional) SKU capacity")]
        [CommandOption("--sku-capacity")]
        public int? SkuCapacity { get; set; }

        [CommandOptionDefinition("(Optional) publisher email address")]
        [CommandOption("--publisher-email")]
        public string? PublisherEmail { get; set; }

        [CommandOptionDefinition("(Optional) publisher name")]
        [CommandOption("--publisher-name")]
        public string? PublisherName { get; set; }
    }
}
