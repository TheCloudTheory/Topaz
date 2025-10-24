using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class DeleteTableCommand(ITopazLogger logger) : Command<DeleteTableCommand.DeleteTableCommandSettings>
{
    public override int Execute(CommandContext context, DeleteTableCommandSettings settings)
    {
        logger.LogInformation($"Deleting table {settings.Name}...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new TableServiceControlPlane(new TableResourceProvider(logger), logger);
        rp.DeleteTable(subscriptionIdentifier, resourceGroupIdentifier, settings.Name, settings.AccountName);

        logger.LogInformation("Table deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteTableCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Table name can't be null.");
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
    public sealed class DeleteTableCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;
        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}