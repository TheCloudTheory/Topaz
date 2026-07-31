using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands;

public sealed class GenericApiManagementCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("apim", apim =>
        {
            apim.AddCommand<CreateOrUpdateApiManagementServiceCommand>("create");
            apim.AddCommand<GetApiManagementServiceCommand>("show");
            apim.AddCommand<DeleteApiManagementServiceCommand>("delete");
            apim.AddCommand<ListApiManagementServicesCommand>("list");
            apim.AddCommand<UpdateApiManagementServiceCommand>("update");
            apim.AddCommand<CheckApiManagementServiceNameAvailabilityCommand>("check-name");
        });
    }
}
