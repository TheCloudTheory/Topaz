using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage queue show", "azure-storage/queue", "Shows properties of a queue in a storage account.")]
[CommandExample("Show a queue", "topaz storage queue show \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"myqueue\"")]
public sealed class ShowQueueCommand(ITopazLogger logger) : Command<ShowQueueCommand.ShowQueueCommandSettings>
{
    public override int Execute(CommandContext context, ShowQueueCommandSettings settings)
    {
        logger.LogInformation($"Getting queue '{settings.Name}'...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new QueueServiceControlPlane(new QueueResourceProvider(logger), logger);
        var propsOp = controlPlane.GetQueueProperties(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, settings.Name!);

        if (propsOp.Result != OperationResult.Success || propsOp.Resource == null)
        {
            logger.LogError($"Queue '{settings.Name}' not found.");
            return 1;
        }

        var props = propsOp.Resource;
        logger.LogInformation($"Name: {props.Name}");
        logger.LogInformation($"Created: {props.CreatedTime:O}");
        logger.LogInformation($"Updated: {props.UpdatedTime:O}");
        logger.LogInformation($"Message Count: {props.ApproximateMessageCount}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ShowQueueCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Queue name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ShowQueueCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
