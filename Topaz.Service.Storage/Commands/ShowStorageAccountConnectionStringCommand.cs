using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class ShowStorageAccountConnectionStringCommand(ITopazLogger logger) : Command<ShowStorageAccountConnectionStringCommand.ShowStorageAccountConnectionStringCommandSettings>
{
    public override int Execute(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
    {
        logger.LogInformation("Listing storage account connection strings...");

        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        var operation = controlPlane.Get(settings.Name!);

        if (operation.result == OperationResult.Failed || operation.resource == null)
        {
            logger.LogError("Failed to get storage account connection string");
            return 1;
        }
        
        var connectionString = new StorageAccountConnectionString(settings.Name!, operation.resource.Keys[0].Value);
        
        logger.LogInformation(connectionString.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowStorageAccountConnectionStringCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ShowStorageAccountConnectionStringCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string? Name { get; set; }
        
    }
}