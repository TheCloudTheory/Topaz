using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group create", "group", "Creates a new resource group.")]
[CommandExample("Create a resource group", "topaz group create \\\n    --name \"my-rg\" \\\n    --location \"eastus\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CreateResourceGroupCommand(HttpClient httpClient, DefaultsProvider provider) : TopazHttpCommand<CreateResourceGroupCommand.CreateResourceGroupCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.Name}";
        var (success, body) = await PutAsync(url, new { location = settings.Location });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateResourceGroupCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.Location ??= defaults.Location;
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Resource group location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Azure region for the resource group.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
