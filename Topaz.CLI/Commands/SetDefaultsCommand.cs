using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
[CommandDefinition("configure set", "generic", "Sets default values for the CLI.")]
[CommandExample("Set default subscription, resource group, and location", "topaz configure set \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-resource-group\" \\\n    --location \"eastus\"")]
internal sealed class SetDefaultsCommand(DefaultsProvider provider) : AsyncCommand<SetDefaultsCommand.SetDefaultsCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, SetDefaultsCommandSettings settings)
    {
        var defaults = FromSettings(settings);
        provider.UpdateDefaults(defaults);
        AnsiConsole.MarkupLine("[green]Defaults updated successfully.[/]");
        return Task.FromResult(0);
    }
    
    public static DefaultValuesModel FromSettings(SetDefaultsCommand.SetDefaultsCommandSettings settings)
    {
        return new DefaultValuesModel
        {
            SubscriptionId = settings.SubscriptionId,
            ResourceGroup = settings.ResourceGroup,
            Location = settings.Location
        };
    }
    
    [UsedImplicitly]
    public sealed class SetDefaultsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("Default subscription ID.", required: false)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Default resource group name", required: false)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("Default location", required: false)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }
    }
}