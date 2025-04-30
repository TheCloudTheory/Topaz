using Azure.Local.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Azure.Local.Service.Storage.Commands;

public sealed class CreateStorageAccountCommand : Command<CreateStorageAccountCommand.CreateStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        PrettyLogger.LogInformation("Creating storage account...");

        var rp = new ResourceProvider();
        var sa = rp.Create(settings.Name!);

        PrettyLogger.LogInformation(sa.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Storage account name can't be null.");
        }

        return base.Validate(context, settings);
    }

    public sealed class CreateStorageAccountCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
