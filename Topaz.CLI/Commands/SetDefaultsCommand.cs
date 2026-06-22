using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.CLI.Commands;

[UsedImplicitly]
internal sealed class SetDefaultsCommand(DefaultsProvider provider, ITopazLogger logger) : AsyncCommand<SetDefaultsCommand.SetDefaultsCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, SetDefaultsCommandSettings settings)
    {
        try
        {
            var defaults = FromSettings(settings);
            provider.UpdateDefaults(defaults);
        
            AnsiConsole.MarkupLine("[green]Defaults updated successfully.[/]");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            
            AnsiConsole.MarkupLine("[red]Host is not running.[/] Start it with [bold]topaz-host start[/].");
            return Task.FromResult(1);
        }
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