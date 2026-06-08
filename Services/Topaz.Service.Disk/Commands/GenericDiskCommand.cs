using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

public sealed class GenericDiskCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("disk", disk =>
        {
            disk.AddCommand<CreateDiskCommand>("create");
            disk.AddCommand<GetDiskCommand>("show");
            disk.AddCommand<DeleteDiskCommand>("delete");
            disk.AddCommand<UpdateDiskCommand>("update");
            disk.AddCommand<ListDisksCommand>("list");
        });
    }
}
