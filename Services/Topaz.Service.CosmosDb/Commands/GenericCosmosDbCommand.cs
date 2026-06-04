using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

public sealed class GenericCosmosDbCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("cosmosdb", cosmosDb =>
        {
            cosmosDb.AddBranch("account", account =>
            {
                account.AddCommand<CreateOrUpdateDatabaseAccountCommand>("create");
                account.AddCommand<GetDatabaseAccountCommand>("get");
                account.AddCommand<DeleteDatabaseAccountCommand>("delete");
                account.AddCommand<UpdateDatabaseAccountCommand>("update");
                account.AddCommand<ListDatabaseAccountsByResourceGroupCommand>("list");
                account.AddCommand<ListDatabaseAccountsBySubscriptionCommand>("list-by-subscription");
                account.AddCommand<ListKeysDatabaseAccountCommand>("list-keys");
                account.AddCommand<ListReadOnlyKeysDatabaseAccountCommand>("list-readonly-keys");
                account.AddCommand<ListConnectionStringsDatabaseAccountCommand>("list-connection-strings");
            });
            cosmosDb.AddBranch("sql-database", sqlDatabase =>
            {
                sqlDatabase.AddCommand<CreateOrUpdateSqlDatabaseCommand>("create");
                sqlDatabase.AddCommand<GetSqlDatabaseCommand>("get");
                sqlDatabase.AddCommand<DeleteSqlDatabaseCommand>("delete");
                sqlDatabase.AddCommand<ListSqlDatabasesCommand>("list");
                sqlDatabase.AddCommand<GetSqlDatabaseThroughputCommand>("get-throughput");
                sqlDatabase.AddCommand<UpdateSqlDatabaseThroughputCommand>("update-throughput");
            });
        });
    }
}
