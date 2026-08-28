using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.EventGrid.Commands.ControlPlane.Namespace;

namespace Topaz.Service.EventGrid.Commands;

public sealed class GenericEventGridCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("eventgrid", eventgrid =>
        {
            eventgrid.AddBranch("namespace", @namespace =>
            {
                @namespace.AddCommand<CreateOrUpdateEventGridNamespaceCommand>("create");
                @namespace.AddCommand<GetEventGridNamespaceCommand>("show");
                @namespace.AddCommand<DeleteEventGridNamespaceCommand>("delete");
                @namespace.AddCommand<UpdateEventGridNamespaceCommand>("update");
                @namespace.AddCommand<ListEventGridNamespaceByResourceGroupCommand>("list-resource-group");
                @namespace.AddCommand<ListEventGridNamespaceBySubscriptionCommand>("list-subscription");
                @namespace.AddCommand<ListEventGridNamespaceKeysCommand>("list-keys");
                @namespace.AddCommand<RegenerateEventGridNamespaceKeyCommand>("regenerate-key");
            });
        });
    }
}