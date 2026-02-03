using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
[CommandDefinition("eventhubs eventhub delete",  "event-hub", "Deletes an Event Hub.")]
[CommandExample("Deletes Event Hub", "topaz eventhubs eventhub delete \\\n    --namespace-name \"sb-namespace\" \\\n    --name \"ehtest\"")]
public class DeleteEventHubCommand(ITopazLogger logger) : Command<DeleteEventHubCommand.DeleteEventHubCommandSettings>
{
    public override int Execute(CommandContext context, DeleteEventHubCommandSettings settings)
    {
        logger.LogDebug(nameof(DeleteEventHubCommand), nameof(Execute), "Executing {0}.{1}.", nameof(DeleteEventHubCommand), nameof(Execute));
        logger.LogInformation($"Deleting {settings.Name} event hub...");

        var controlPlane = new EventHubServiceControlPlane(new EventHubResourceProvider(logger), logger);
        controlPlane.Delete(settings.Name!, EventHubNamespaceIdentifier.From(settings.NamespaceName!));

        logger.LogInformation($"Event hub {settings.Name} deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteEventHubCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Name can't be null.");
        }
        
        return string.IsNullOrEmpty(settings.NamespaceName) 
            ? ValidationResult.Error("Namespace name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteEventHubCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Event Hub name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOptionDefinition("(Required) Event Hub namespace name.")]
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
    }
}