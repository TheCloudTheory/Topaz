using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteEventHubNamespaceCommand(ITopazLogger logger) : Command<DeleteEventHubNamespaceCommand.DeleteEventHubNamespaceCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, DeleteEventHubNamespaceCommandSettings settings)
    {
        this._topazLogger.LogInformation("Deleting Azure Event Hub Namespace...");

        var rp = new ResourceProvider(this._topazLogger);
        rp.Delete(settings.Name!);

        this._topazLogger.LogInformation("Azure Event Hub Namespace deleted.");

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