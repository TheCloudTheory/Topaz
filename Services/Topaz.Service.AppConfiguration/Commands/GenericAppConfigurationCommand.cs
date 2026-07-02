using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.AppConfiguration.Commands.Kv;

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

            appconfig.AddBranch("kv", kv =>
            {
                kv.AddCommand<SetKeyValueCommand>("set");
                kv.AddCommand<GetKeyValueCommand>("show");
                kv.AddCommand<ListKeyValuesCommand>("list");
                kv.AddCommand<DeleteKeyValueCommand>("delete");
                kv.AddCommand<LockKeyValueCommand>("lock");
                kv.AddCommand<UnlockKeyValueCommand>("unlock");
                kv.AddCommand<ListLabelsCommand>("list-labels");
                kv.AddCommand<ListRevisionsCommand>("list-revisions");
            });
        });
    }
}
