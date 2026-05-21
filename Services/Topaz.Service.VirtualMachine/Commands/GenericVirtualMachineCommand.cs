using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.VirtualMachine.Commands;

public sealed class GenericVirtualMachineCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("vm", vm =>
        {
            vm.AddCommand<CreateVirtualMachineCommand>("create");
            vm.AddCommand<GetVirtualMachineCommand>("show");
            vm.AddCommand<DeleteVirtualMachineCommand>("delete");
            vm.AddCommand<ListVirtualMachinesCommand>("list");
        });
    }
}
