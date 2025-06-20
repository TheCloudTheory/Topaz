using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Commands;

[UsedImplicitly]
public sealed class DeleteServiceBusNamespaceCommand(ITopazLogger logger) : Command<DeleteServiceBusNamespaceCommand.DeleteServiceBusCommandSettings>
{
    public override int Execute(CommandContext context, DeleteServiceBusCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(DeleteServiceBusNamespaceCommand)}.{nameof(Execute)}.");
        
        var controlPlane = new ServiceBusServiceControlPlane(new ResourceProvider(logger), logger);
        _ = controlPlane.DeleteNamespace(settings.Name!);

        logger.LogInformation("Service Bus namespace deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteServiceBusCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Service Bus namespace name can't be null.");
        }

        return base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class DeleteServiceBusCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}