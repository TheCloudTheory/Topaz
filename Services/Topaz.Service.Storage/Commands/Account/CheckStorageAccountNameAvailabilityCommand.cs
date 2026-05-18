using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account check-name", "azure-storage/account", "Checks whether a storage account name is available.")]
[CommandExample("Check name availability", "topaz storage account check-name \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --name \"salocal\"")]
public sealed class CheckStorageAccountNameAvailabilityCommand(HttpClient httpClient)
    : TopazHttpCommand<CheckStorageAccountNameAvailabilityCommand.CheckStorageAccountNameAvailabilityCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CheckStorageAccountNameAvailabilityCommandSettings settings)
    {
        AnsiConsole.WriteLine("Checking storage account name availability...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Storage/checkNameAvailability";
        var (success, body) = await PostAsync(url, new { name = settings.Name, type = "Microsoft.Storage/storageAccounts" });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckStorageAccountNameAvailabilityCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckStorageAccountNameAvailabilityCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name to check.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;
    }
}
