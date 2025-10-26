using Spectre.Console.Cli;
using Topaz.Service.Shared.Command;

namespace Topaz.Service.EventHub.Commands;

public sealed class GenericEventHubCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("eventhubs", branch =>
        {
            branch.AddBranch("namespace", @namespace =>
            {
                @namespace.AddCommand<CreateEventHubNamespaceCommand>("create");
                @namespace.AddCommand<DeleteEventHubNamespaceCommand>("delete");
            });
                
            branch.AddBranch("eventhub", eventHub =>
            {
                eventHub.AddCommand<CreateEventHubCommand>("create");
                eventHub.AddCommand<DeleteEventHubCommand>("delete");
            });
        });
    }
}