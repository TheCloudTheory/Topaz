using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

[UsedImplicitly]
public class DeleteEventHubNamespaceCommand(ITopazLogger logger) : Command<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>
{
    public override int Execute(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Event Hub Namespace...");

        var rp = new ResourceProvider(logger);
        rp.Delete(settings.Name!);

        logger.LogInformation("Azure Event Hub Namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.Name) 
            ? ValidationResult.Error("Azure Event Hub Namespace name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteEventHubNamespaceCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}