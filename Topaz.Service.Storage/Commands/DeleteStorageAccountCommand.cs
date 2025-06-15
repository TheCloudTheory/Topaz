using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public class DeleteStorageAccountCommand(ITopazLogger logger) : Command<DeleteStorageAccountCommand.DeleteStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        logger.LogInformation("Deleting storage account...");

        var rp = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        rp.Delete(settings.Name!);

        logger.LogInformation("Storage account deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
