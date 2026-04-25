using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message update", "azure-storage/queue", "Updates the visibility timeout and/or content of a dequeued message.")]
[CommandExample("Update message visibility", "topaz storage message update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --message-id \"<id>\" \\\n    --pop-receipt \"<receipt>\" \\\n    --visibility-timeout 60")]
public sealed class UpdateMessageCommand(ITopazLogger logger) : Command<UpdateMessageCommand.UpdateMessageCommandSettings>
{
    public override int Execute(CommandContext context, UpdateMessageCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var dataPlane = QueueServiceDataPlane.New(logger);

        var result = dataPlane.PutMessage(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, settings.QueueName!, settings.MessageId!,
            settings.Content ?? string.Empty, settings.VisibilityTimeout);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Message '{settings.MessageId}' not found in queue '{settings.QueueName}'.");
            return 1;
        }

        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            logger.LogError($"Failed to update message: {result.Reason}");
            return 1;
        }

        logger.LogInformation($"PopReceipt:  {result.Resource.PopReceipt}");
        logger.LogInformation($"NextVisible: {result.Resource.NextVisibleTime:O}");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateMessageCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.QueueName))
            return ValidationResult.Error("Queue name can't be null.");
        if (string.IsNullOrEmpty(settings.MessageId))
            return ValidationResult.Error("Message ID can't be null.");
        if (string.IsNullOrEmpty(settings.PopReceipt))
            return ValidationResult.Error("Pop receipt can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateMessageCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-q|--queue-name")] public string? QueueName { get; set; }
        [CommandOptionDefinition("(Required) Message ID.", required: true)]
        [CommandOption("-i|--message-id")] public string? MessageId { get; set; }
        [CommandOptionDefinition("(Required) Pop receipt obtained when the message was dequeued.", required: true)]
        [CommandOption("-r|--pop-receipt")] public string? PopReceipt { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("New visibility timeout in seconds (0–604800). Default: 30.")]
        [CommandOption("--visibility-timeout")] public int VisibilityTimeout { get; set; } = 30;
        [CommandOptionDefinition("Updated message content. If omitted, content is preserved.")]
        [CommandOption("-c|--content")] public string? Content { get; set; }
    }
}
