using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Storage.Commands.Blob;

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
                table.AddCommand<ListTablesCommand>("list");
                table.AddCommand<ShowTableCommand>("show");
                table.AddCommand<InsertTableEntityCommand>("insert-entity");
                table.AddCommand<DeleteTableEntityCommand>("delete-entity");
                table.AddCommand<QueryTableEntitiesCommand>("query-entity");
            });

            branch.AddBranch("container", container =>
            {
                container.AddCommand<CreateBlobContainerCommand>("create");
                container.AddCommand<ListBlobContainersCommand>("list");
                container.AddCommand<DeleteBlobContainerCommand>("delete");

                container.AddBranch("metadata", metadata =>
                {
                    metadata.AddCommand<SetContainerMetadataCommand>("set");
                });
            });

            branch.AddBranch("blob", blob =>
            {
                blob.AddCommand<UploadBlobCommand>("upload");
                blob.AddCommand<DownloadBlobCommand>("download");
                blob.AddCommand<DeleteBlobCommand>("delete");
                blob.AddCommand<ListBlobsCommand>("list");
                blob.AddCommand<ShowBlobCommand>("show");
                blob.AddCommand<SetBlobPropertiesCommand>("update");
                blob.AddCommand<CopyBlobCommand>("copy");
                blob.AddCommand<LeaseBlobCommand>("lease");
                blob.AddCommand<SnapshotBlobCommand>("snapshot");
                blob.AddCommand<UndeleteBlobCommand>("undelete");

                blob.AddBranch("metadata", metadata =>
                {
                    metadata.AddCommand<GetBlobMetadataCommand>("show");
                    metadata.AddCommand<SetBlobMetadataCommand>("update");
                });
            });

            branch.AddBranch("queue", queue =>
            {
                queue.AddCommand<CreateQueueCommand>("create");
                queue.AddCommand<DeleteQueueCommand>("delete");
                queue.AddCommand<ListQueuesCommand>("list");
                queue.AddCommand<ShowQueueCommand>("show");
            });
        });
    }
}