using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

public class DeleteEventHubCommand(ILogger logger) : Command<DeleteEventHubCommand.DeleteEventHubCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, DeleteEventHubCommandSettings settings)
    {
        this.logger.LogDebug($"Executing {nameof(CreateEventHubCommand)}.{nameof(Execute)}.");
        this.logger.LogInformation($"Deleting {settings.Name} event hub...");

        var controlPlane = new EventHubControlPlane(new ResourceProvider(this.logger), logger);
        controlPlane.Delete(settings.Name!, settings.NamespaceName!);

        this.logger.LogInformation($"Event hub {settings.Name} deleted.");

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
    
    public sealed class DeleteEventHubCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
    }
}