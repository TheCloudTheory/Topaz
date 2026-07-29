using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.VirtualMachine.Commands.AvailabilitySets;

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
            vm.AddCommand<UpdateVirtualMachineCommand>("update");
            vm.AddBranch("image-version", imageVersion =>
            {
                imageVersion.AddCommand<GetVirtualMachineImageVersionCommand>("get");
                imageVersion.AddCommand<ListVirtualMachineImageVersionsCommand>("list");
            });
            vm.AddBranch("availability-set", availabilitySet =>
            {
                availabilitySet.AddCommand<CreateOrUpdateAvailabilitySetCommand>("create");
                availabilitySet.AddCommand<GetAvailabilitySetCommand>("show");
                availabilitySet.AddCommand<DeleteAvailabilitySetCommand>("delete");
                availabilitySet.AddCommand<ListAvailabilitySetsCommand>("list");
                availabilitySet.AddCommand<UpdateAvailabilitySetCommand>("update");
                availabilitySet.AddCommand<ListAvailabilitySetsBySubscriptionCommand>("list-by-subscription");
                availabilitySet.AddCommand<ListAvailableSizesCommand>("list-available-sizes");
            });
        });
    }
}
