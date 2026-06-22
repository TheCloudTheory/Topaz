using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualNetwork.Commands;

public sealed class GenericPublicIpAddressCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("pip", pip =>
        {
            pip.AddCommand<CreatePublicIpAddressCommand>("create");
            pip.AddCommand<GetPublicIpAddressCommand>("show");
            pip.AddCommand<DeletePublicIpAddressCommand>("delete");
            pip.AddCommand<ListPublicIpAddressesCommand>("list");
        });
    }
}
