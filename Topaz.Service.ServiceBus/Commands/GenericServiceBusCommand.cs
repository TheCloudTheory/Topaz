using Spectre.Console.Cli;
using Topaz.Service.Shared.Command;

namespace Topaz.Service.ServiceBus.Commands;

public sealed class GenericServiceBusCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("servicebus", branch =>
        {
            branch.AddBranch("namespace", @namespace =>
            {
                @namespace.AddCommand<CreateServiceBusNamespaceCommand>("create");
                @namespace.AddCommand<DeleteServiceBusNamespaceCommand>("delete");
            });
                
            branch.AddBranch("queue", queue =>
            {
                queue.AddCommand<CreateServiceBusQueueCommand>("create");
                queue.AddCommand<DeleteServiceBusQueueCommand>("delete");
            });
        });
    }
}