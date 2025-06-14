using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

public sealed class CreateEventHubCommand(ITopazLogger logger) : Command<CreateEventHubCommand.CreateEventHubCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        this._topazLogger.LogInformation($"Executing {nameof(CreateEventHubCommand)}.{nameof(Execute)}.");

        var controlPlane = new EventHubControlPlane(new ResourceProvider(this._topazLogger), _topazLogger);
        var eh = controlPlane.Create(settings.Name!, settings.NamespaceName!);

        this._topazLogger.LogInformation(eh.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Name can't be null.");
        }
        
        return string.IsNullOrEmpty(settings.NamespaceName) 
            ? ValidationResult.Error("Namespace name can't be null.") : base.Validate(context, settings);
    }
    
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
        
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
    }
}