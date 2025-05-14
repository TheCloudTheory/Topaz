using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteEventHubNamespaceCommand(ILogger logger) : Command<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        this.logger.LogInformation("Deleting Azure Event Hub Namespace...");

        var rp = new ResourceProvider(this.logger);
        rp.Delete(settings.Name!);

        this.logger.LogInformation("Azure Event Hub Namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.Name) 
            ? ValidationResult.Error("Azure Event Hub Namespace name can't be null.") : base.Validate(context, settings);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DeleteEventHubNamespaceCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}