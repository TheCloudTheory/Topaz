using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Storage.Commands;

public class DeleteStorageAccountCommand(ILogger logger) : Command<DeleteStorageAccountCommand.DeleteStorageAccountCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, DeleteStorageAccountCommandSettings settings)
    {
        this.logger.LogInformation("Deleting storage account...");

        var rp = new AzureStorageControlPlane(new ResourceProvider(this.logger), this.logger);
        rp.Delete(settings.Name!);

        this.logger.LogInformation("Storage account deleted.");

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

    public sealed class DeleteStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
