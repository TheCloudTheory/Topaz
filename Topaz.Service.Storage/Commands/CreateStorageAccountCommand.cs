using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class CreateStorageAccountCommand(ITopazLogger logger) : Command<CreateStorageAccountCommand.CreateStorageAccountCommandSettings>
{
    public override int Execute(CommandContext context, CreateStorageAccountCommandSettings settings)
    {
        logger.LogInformation("Creating storage account...");

        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        var sa = controlPlane.Create(settings.Name!, settings.ResourceGroup!, settings.Location!, settings.SubscriptionId!);

        if (sa.result == OperationResult.Failed || sa.resource == null)
        {
            return 1;
        }

        logger.LogInformation(sa.resource.ToString());

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
