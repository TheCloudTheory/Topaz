using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message get", "azure-storage/queue", "Dequeues one or more messages from a queue, hiding them for the visibility timeout duration.")]
[CommandExample("Dequeue up to 5 messages", "topaz storage message get \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --num-messages 5")]
public sealed class GetMessagesCommand(ITopazLogger logger) : Command<GetMessagesCommand.GetMessagesCommandSettings>
{
    public override int Execute(CommandContext context, GetMessagesCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var dataPlane = QueueServiceDataPlane.New(logger);

        var result = dataPlane.GetMessages(subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!, settings.QueueName!, settings.NumMessages, settings.VisibilityTimeout);

        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            logger.LogError($"Failed to retrieve messages: {result.Reason}");
            return 1;
        }

        if (result.Resource.Count == 0)
        {
            logger.LogInformation("No messages available.");
            return 0;
        }

        foreach (var msg in result.Resource)
        {
            logger.LogInformation($"MessageId:     {msg.Id}");
            logger.LogInformation($"PopReceipt:    {msg.PopReceipt}");
            logger.LogInformation($"Content:       {msg.Content}");
            logger.LogInformation($"DequeueCount:  {msg.DequeueCount}");
            logger.LogInformation($"InsertedAt:    {msg.EnqueuedTime:O}");
            logger.LogInformation($"NextVisible:   {msg.NextVisibleTime:O}");
            logger.LogInformation("---");
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetMessagesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.QueueName))
            return ValidationResult.Error("Queue name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (settings.NumMessages is < 1 or > 32)
            return ValidationResult.Error("--num-messages must be between 1 and 32.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetMessagesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-q|--queue-name")] public string? QueueName { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("Number of messages to retrieve (1–32). Default: 1.")]
        [CommandOption("-n|--num-messages")] public int NumMessages { get; set; } = 1;
        [CommandOptionDefinition("Seconds messages are hidden after dequeue (0–604800). Default: 30.")]
        [CommandOption("--visibility-timeout")] public int VisibilityTimeout { get; set; } = 30;
    }
}
