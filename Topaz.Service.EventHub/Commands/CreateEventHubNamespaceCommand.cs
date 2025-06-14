using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

public sealed class CreateEventHubNamespaceCommand(ITopazLogger logger) : Command<CreateEventHubNamespaceCommand.CreateEventHubCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        this._topazLogger.LogInformation($"Executing {nameof(CreateEventHubNamespaceCommand)}.{nameof(Execute)}.");

        var controlPlane = new EventHubControlPlane(new ResourceProvider(this._topazLogger), _topazLogger);
        var ns = controlPlane.CreateNamespace(settings.Name!, settings.ResourceGroup!, settings.Location!, settings.SubscriptionId!);

        this._topazLogger.LogInformation(ns.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
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
    
    public sealed class CreateEventHubCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOption("-s|--subscriptionId")]
        public string? SubscriptionId { get; set; }
    }
}