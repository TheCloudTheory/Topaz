using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Commands;

public sealed class CreateEventHubCommand(ILogger logger) : Command<CreateEventHubCommand.CreateEventHubCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, CreateEventHubCommandSettings settings)
    {
        this.logger.LogInformation($"Executing {nameof(CreateEventHubCommand)}.{nameof(Execute)}.");

        var controlPlane = new EventHubControlPlane(new ResourceProvider(this.logger), logger);
        var kv = controlPlane.Create(settings.Name!, settings.ResourceGroup!, settings.Location!, settings.SubscriptionId!);

        this.logger.LogInformation(kv.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateEventHubCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Location can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.NamespaceName))
        {
            return ValidationResult.Error("Namespace name can't be null.");
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
        
        [CommandOption("--namespace-name")]
        public string? NamespaceName { get; set; }
    }
}