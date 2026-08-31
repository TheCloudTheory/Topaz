using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.EventGrid.Commands.ControlPlane.Namespace;
using Topaz.Service.EventGrid.Commands.ControlPlane.Topic;

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

            eventgrid.AddBranch("topic", topic =>
            {
                topic.AddCommand<CreateOrUpdateEventGridTopicCommand>("create");
                topic.AddCommand<GetEventGridTopicCommand>("show");
                topic.AddCommand<DeleteEventGridTopicCommand>("delete");
                topic.AddCommand<UpdateEventGridTopicCommand>("update");
                topic.AddCommand<ListEventGridTopicByResourceGroupCommand>("list-resource-group");
                topic.AddCommand<ListEventGridTopicBySubscriptionCommand>("list-subscription");
                topic.AddCommand<ListEventGridTopicEventTypesCommand>("list-event-types");
                topic.AddCommand<ListEventGridTopicKeysCommand>("list-keys");
                topic.AddCommand<RegenerateEventGridTopicKeyCommand>("regenerate-key");
            });
        });
    }
}