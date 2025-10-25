using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class CreateBlobContainerCommand(ITopazLogger logger) : Command<CreateBlobContainerCommand.CreateBlobContainerCommandSettings>
{
    public override int Execute(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        logger.LogInformation("Creating blob container...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        _ = rp.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, settings.Name, settings.AccountName);

        logger.LogInformation("Container created.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Container name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Storage account subscription ID can't be null.");
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
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}