using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Sql.Commands;

public sealed class GenericSqlCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("sql", sql =>
        {
            sql.AddCommand<CreateSqlServerCommand>("create");
            sql.AddCommand<GetSqlServerCommand>("show");
            sql.AddCommand<DeleteSqlServerCommand>("delete");
            sql.AddCommand<ListSqlServersCommand>("list");
        });
    }
}
