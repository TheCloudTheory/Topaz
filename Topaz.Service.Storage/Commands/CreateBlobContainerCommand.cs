using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class CreateBlobContainerCommand(ILogger logger) : Command<CreateBlobContainerCommand.CreateBlobContainerCommandSettings>
{
    public override int Execute(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        logger.LogInformation("Creating blob container...");

        var rp = new BlobServiceControlPlane(new BlobResourceProvider(logger), logger);
        _ = rp.CreateContainer(settings.Name, settings.AccountName);

        logger.LogInformation("Container created.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Table name can't be null.");
        }

        return string.IsNullOrEmpty(settings.AccountName) ? 
            ValidationResult.Error("Storage account name can't be null.") 
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateBlobContainerCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;

        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;
    }
}