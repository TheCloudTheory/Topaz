using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppConfiguration.Commands;

public sealed class GenericAppConfigurationCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("appconfig", appconfig =>
        {
            appconfig.AddCommand<CreateAppConfigurationStoreCommand>("create");
            appconfig.AddCommand<GetAppConfigurationStoreCommand>("show");
            appconfig.AddCommand<DeleteAppConfigurationStoreCommand>("delete");
            appconfig.AddCommand<UpdateAppConfigurationStoreCommand>("update");
            appconfig.AddCommand<ListAppConfigurationStoresCommand>("list");
            appconfig.AddCommand<ListAppConfigurationKeysCommand>("list-keys");
            appconfig.AddCommand<RegenerateAppConfigurationKeyCommand>("regenerate-key");
        });
    }
}
