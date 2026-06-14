using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.LoadBalancer.Commands;

public sealed class GenericLoadBalancerCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("lb", lb =>
        {
            lb.AddCommand<CreateLoadBalancerCommand>("create");
            lb.AddCommand<GetLoadBalancerCommand>("show");
            lb.AddCommand<DeleteLoadBalancerCommand>("delete");
            lb.AddCommand<UpdateLoadBalancerCommand>("update");
            lb.AddCommand<ListLoadBalancersCommand>("list");
        });
    }
}
