using Spectre.Console.Cli;
using Topaz.Service.Shared.Command;

namespace Topaz.Service.Subscription.Commands;

public sealed class GenericSubscriptionCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("subscription", subscription => {
            subscription.AddCommand<CreateSubscriptionCommand>("create");
            subscription.AddCommand<DeleteSubscriptionCommand>("delete");
            subscription.AddCommand<ListSubscriptionsCommand>("list");
        });
    }
}