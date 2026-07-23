using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.VirtualNetwork.Commands.PrivateEndpoints;
using Topaz.Service.VirtualNetwork.Commands.Subnets;

namespace Topaz.Service.VirtualNetwork.Commands;

public sealed class GenericVirtualNetworkCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("vnet", vnet =>
        {
            vnet.AddCommand<CreateVirtualNetworkCommand>("create");
            vnet.AddCommand<GetVirtualNetworkCommand>("show");
            vnet.AddCommand<DeleteVirtualNetworkCommand>("delete");
            vnet.AddCommand<ListVirtualNetworksCommand>("list");
            vnet.AddCommand<CheckIpAddressAvailabilityCommand>("check-ip");
            vnet.AddBranch("subnet", subnet =>
            {
                subnet.AddCommand<CreateSubnetCommand>("create");
                subnet.AddCommand<GetSubnetCommand>("show");
                subnet.AddCommand<ListSubnetsCommand>("list");
                subnet.AddCommand<DeleteSubnetCommand>("delete");
            });
            vnet.AddBranch("private-endpoint", subnet =>
            {
                subnet.AddCommand<CreateOrUpdatePrivateEndpointCommand>("create");
                subnet.AddCommand<GetPrivateEndpointCommand>("show");
                subnet.AddCommand<ListPrivateEndpointsByResourceGroupCommand>("list");
                subnet.AddCommand<ListPrivateEndpointsBySubscriptionCommand>("list-by-subscription");
                subnet.AddCommand<DeletePrivateEndpointCommand>("delete");
            });
        });
    }
}
