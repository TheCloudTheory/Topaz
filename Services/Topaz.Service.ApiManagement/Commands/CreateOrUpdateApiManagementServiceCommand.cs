using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands;

[UsedImplicitly]
[CommandDefinition("apim create", "api-management", "Creates or updates an Azure API Management service.")]
[CommandExample("Creates a new API Management service",
    "topaz apim create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-apim\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --publisher-email \"admin@example.com\" \\\n    --publisher-name \"My Company\" \\\n    --sku-name \"Developer\" \\\n    --sku-capacity 1")]
internal sealed class CreateOrUpdateApiManagementServiceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateOrUpdateApiManagementServiceCommand.CreateOrUpdateApiManagementServiceCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateOrUpdateApiManagementServiceCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = new
            {
                name = settings.SkuName ?? "Developer",
                capacity = settings.SkuCapacity ?? 1
            },
            properties = new
            {
                publisherEmail = settings.PublisherEmail,
                publisherName = settings.PublisherName
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CreateOrUpdateApiManagementServiceCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("API Management service name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.PublisherEmail))
            return ValidationResult.Error("Publisher email can't be null.");
        if (string.IsNullOrEmpty(settings.PublisherName))
            return ValidationResult.Error("Publisher name can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateOrUpdateApiManagementServiceCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) location")]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) publisher email address")]
        [CommandOption("--publisher-email")]
        public string? PublisherEmail { get; set; }

        [CommandOptionDefinition("(Required) publisher name")]
        [CommandOption("--publisher-name")]
        public string? PublisherName { get; set; }

        [CommandOptionDefinition("SKU name (e.g. Developer, Basic, Standard, Premium)")]
        [CommandOption("--sku-name")]
        public string? SkuName { get; set; }

        [CommandOptionDefinition("SKU capacity")]
        [CommandOption("--sku-capacity")]
        public int? SkuCapacity { get; set; }
    }
}
