using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ApiManagement.Commands.Api;
using Topaz.Service.ApiManagement.Commands.Backend;
using Topaz.Service.ApiManagement.Commands.Product;

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

            apim.AddBranch("api", api =>
            {
                api.AddCommand<GetApiCommand>("show");
                api.AddCommand<CreateOrUpdateApiCommand>("create");
                api.AddCommand<UpdateApiCommand>("update");
                api.AddCommand<DeleteApiCommand>("delete");
                api.AddCommand<ListApiByServiceCommand>("list");
                api.AddCommand<GetApiEntityTagCommand>("get-entity-tag");
                api.AddCommand<ListApiRevisionsByServiceCommand>("list-revisions");
            });

            apim.AddBranch("product", product =>
            {
                product.AddCommand<GetProductCommand>("show");
                product.AddCommand<CreateOrUpdateProductCommand>("create");
                product.AddCommand<UpdateProductCommand>("update");
                product.AddCommand<DeleteProductCommand>("delete");
                product.AddCommand<ListProductByServiceCommand>("list");
                product.AddCommand<GetProductEntityTagCommand>("get-entity-tag");
                product.AddCommand<CheckProductApiAssignmentExistCommand>("check-api");
                product.AddCommand<CreateOrUpdateProductApiCommand>("add-api");
                product.AddCommand<DeleteProductApiCommand>("remove-api");
                product.AddCommand<ListApiAssignmentsByProductCommand>("list-apis");
            });

            apim.AddBranch("backend", backend =>
            {
                backend.AddCommand<GetBackendCommand>("show");
                backend.AddCommand<CreateOrUpdateBackendCommand>("create");
                backend.AddCommand<UpdateBackendCommand>("update");
                backend.AddCommand<DeleteBackendCommand>("delete");
                backend.AddCommand<ListBackendByServiceCommand>("list");
                backend.AddCommand<GetBackendEntityTagCommand>("get-entity-tag");
                backend.AddCommand<ReconnectBackendCommand>("reconnect");
            });
        });
    }
}
