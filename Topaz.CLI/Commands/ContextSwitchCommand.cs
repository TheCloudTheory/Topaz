using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.CLI.Commands;

[CommandDefinition("context switch", "generic", "Changes the active cloud environment context.")]
[CommandExample("Switch the context by selecting one from the list", "topaz context switch")]
[CommandExample("Switch the context by using the one provided by the parameter", "topaz context switch -n Topaz")]
[UsedImplicitly]
internal sealed class ContextSwitchCommand(AzureCliRunner runner) : AsyncCommand<ContextSwitchCommand.Settings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (settings.ContextName is null && settings is { UseTopaz: false, UseDefault: false })
            {
                var listOfContextRaw = runner.RunCommand("cloud list");
                if(string.IsNullOrEmpty(listOfContextRaw))
                {
                    AnsiConsole.WriteLine("No Azure contexts found.");
                    return Task.FromResult(1);
                }
            
                var listOfContext = JsonSerializer.Deserialize<AzureContext[]>(listOfContextRaw, GlobalSettings.JsonOptions);
                if (listOfContext is null or { Length: 0 })
                {
                    AnsiConsole.WriteLine("No Azure contexts found.");
                    return Task.FromResult(1);
                }

                var selected = AnsiConsole.Prompt(
                    new SelectionPrompt<AzureContext>()
                        .Title("Select an Azure context:")
                        .UseConverter(c => c.IsActive ? $"{c.Name} [green](active)[/]" : c.Name ?? string.Empty)
                        .AddChoices(listOfContext));

                _ = runner.RunCommand($"cloud set -n {selected.Name}");
                AnsiConsole.WriteLine("Context switched.");
            }
            else if (settings.UseTopaz)
            {
                runner.RunCommand($"cloud set -n Topaz");
            }
            else if (settings.UseDefault)
            {
                runner.RunCommand($"cloud set -n AzureCloud");
            }
            else
            {
                runner.RunCommand($"cloud set -n {settings.ContextName}");
            }
        
            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            return Task.FromException<int>(exception);
        }
    }
    
    [UsedImplicitly]
    internal class Settings : CommandSettings
    {
        [CommandOption("-n|--name")]
        [CommandOptionDefinition("Name of the context to switch to")]
        public string? ContextName { get; init; }
        
        [CommandOption("--use-topaz")]
        [CommandOptionDefinition("Use the default context provided by Topaz")]
        public bool UseTopaz { get; init; }
        
        [CommandOption("--use-default")]
        [CommandOptionDefinition("Use the default context provided by Azure CLI")]
        public bool UseDefault { get; init; }
    }
}