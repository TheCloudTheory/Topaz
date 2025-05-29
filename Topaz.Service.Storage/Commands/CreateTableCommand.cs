using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

public sealed class CreateTableCommand(ILogger logger) : Command<CreateTableCommand.CreateTableCommandSettings>
{
    public override int Execute(CommandContext context, CreateTableCommandSettings settings)
    {
        logger.LogInformation("Creating table...");

        var rp = new TableServiceControlPlane(new TableResourceProvider(logger), logger);
        var sa = rp.CreateTable(settings.Name, settings.AccountName);

        logger.LogInformation($"Table created: {sa!}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateTableCommandSettings settings)
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
    public sealed class CreateTableCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;

        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;

    }
}