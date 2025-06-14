using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.Storage.Commands;

public sealed class CreateStorageAccountCommand(ITopazLogger logger) : Command<CreateStorageAccountCommand.CreateStorageAccountCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        this._topazLogger.LogInformation("Creating storage account...");

        var rp = new AzureStorageControlPlane(new ResourceProvider(this._topazLogger), this._topazLogger);
        var sa = rp.Create(settings.Name!, settings.ResourceGroup!, settings.Location!, settings.SubscriptionId!);

        this._topazLogger.LogInformation(sa.ToString());

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

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Storage account subscription ID can't be null.");
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

        [CommandOption("-s|--subscriptionId")]
        public string? SubscriptionId { get; set; }
    }
}
