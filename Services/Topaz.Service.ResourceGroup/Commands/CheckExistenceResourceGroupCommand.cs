using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group exists", "group", "Checks whether a resource group exists.")]
[CommandExample("Check if a resource group exists", "topaz group exists \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CheckExistenceResourceGroupCommand(HttpClient httpClient) : TopazHttpCommand<CheckExistenceResourceGroupCommand.CheckExistenceResourceGroupCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CheckExistenceResourceGroupCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.Name}";
        var (success, _) = await GetAsync(url);
        if (success)
            AnsiConsole.WriteLine($"Resource group '{settings.Name}' exists.");
        else
            AnsiConsole.WriteLine($"Resource group '{settings.Name}' does not exist.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckExistenceResourceGroupCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckExistenceResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;
    }
}
