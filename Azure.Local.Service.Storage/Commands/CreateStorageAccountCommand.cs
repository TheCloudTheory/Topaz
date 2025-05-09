using Azure.Local.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Azure.Local.Service.Storage.Commands;

public sealed class CreateStorageAccountCommand(ILogger logger) : Command<CreateStorageAccountCommand.CreateStorageAccountCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        this.logger.LogInformation("Creating storage account...");

        var rp = new ResourceProvider(this.logger);
        var sa = rp.Create(settings.Name!, settings.ResourceGroup!, settings.Location!);

        this.logger.LogInformation(sa.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }

        return base.Validate(context, settings);
    }

    public sealed class CreateStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }
    }
}
