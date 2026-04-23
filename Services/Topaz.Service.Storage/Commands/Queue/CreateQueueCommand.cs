using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage queue create", "azure-storage/queue", "Creates a new queue in a storage account.")]
[CommandExample("Create a queue", "topaz storage queue create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"myqueue\"")]
public sealed class CreateQueueCommand(ITopazLogger logger) : Command<CreateQueueCommand.CreateQueueCommandSettings>
{
    public override int Execute(CommandContext context, CreateQueueCommandSettings settings)
    {
        logger.LogInformation("Creating queue...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new QueueServiceControlPlane(new QueueResourceProvider(logger), logger);
        var queueOp = rp.CreateQueue(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName, settings.Name);

        logger.LogInformation($"Queue created: {queueOp.Resource!.Name}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateQueueCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Queue name can't be null.");
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
    public sealed class CreateQueueCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
