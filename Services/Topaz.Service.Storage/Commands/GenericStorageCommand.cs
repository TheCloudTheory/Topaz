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
                account.AddCommand<DeleteStorageAccountCommand>("delete");
                account.AddCommand<ListStorageAccountsCommand>("list");
                account.AddCommand<ShowStorageAccountConnectionStringCommand>("show-connection-string");
                account.AddCommand<GenerateAccountSasCommand>("generate-sas");

                account.AddBranch("keys", keys =>
                {
                    keys.AddCommand<ListStorageAccountKeysCommand>("list");
                    keys.AddCommand<RegenerateStorageAccountKeyCommand>("renew");
                });
            });

            branch.AddBranch("table", account =>
            {
                account.AddCommand<CreateTableCommand>("create");
                account.AddCommand<DeleteTableCommand>("delete");
            });
                
            branch.AddBranch("container", account =>
            {
                account.AddCommand<CreateBlobContainerCommand>("create");
            });
        });    
    }
}