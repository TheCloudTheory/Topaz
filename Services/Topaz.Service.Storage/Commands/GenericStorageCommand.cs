using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

public sealed class GenericStorageCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("storage", branch =>
        {
            branch.AddBranch("account", account =>
            {
                account.AddCommand<CreateStorageAccountCommand>("create");
                account.AddCommand<ShowStorageAccountCommand>("show");
                account.AddCommand<UpdateStorageAccountCommand>("update");
                account.AddCommand<DeleteStorageAccountCommand>("delete");
                account.AddCommand<ListStorageAccountsCommand>("list");
                account.AddCommand<CheckStorageAccountNameAvailabilityCommand>("check-name");
                account.AddCommand<ShowStorageAccountConnectionStringCommand>("show-connection-string");
                account.AddCommand<GenerateAccountSasCommand>("generate-sas");
                account.AddCommand<GenerateServiceSasCommand>("generate-service-sas");

                account.AddBranch("keys", keys =>
                {
                    keys.AddCommand<ListStorageAccountKeysCommand>("list");
                    keys.AddCommand<RegenerateStorageAccountKeyCommand>("renew");
                });
            });

            branch.AddBranch("table", table =>
            {
                table.AddCommand<CreateTableCommand>("create");
                table.AddCommand<DeleteTableCommand>("delete");
            });

            branch.AddBranch("container", container =>
            {
                container.AddCommand<CreateBlobContainerCommand>("create");
                container.AddCommand<ListBlobContainersCommand>("list");
            });
        });
    }
}