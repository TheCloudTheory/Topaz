using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Redis.Commands;

[UsedImplicitly]
[CommandDefinition("redis create", "redis", "Creates or updates an Azure Cache for Redis instance.")]
[CommandExample("Creates a new Redis cache",
    "topaz redis create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-redis\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --sku \"Standard\"")]
internal sealed class CreateRedisCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateRedisCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Cache/redis/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            sku = settings.Sku == null ? null : new { name = settings.Sku, family = settings.SkuFamily ?? "C", capacity = settings.SkuCapacity ?? 1 },
            properties = new { }
        }, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cache name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Cache name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Location.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. Basic, Standard, Premium).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Optional) SKU family (C or P).", required: false)]
        [CommandOption("--sku-family")]
        public string? SkuFamily { get; set; }

        [CommandOptionDefinition("(Optional) SKU capacity (0-6).", required: false)]
        [CommandOption("--sku-capacity")]
        public int? SkuCapacity { get; set; }
    }
}
