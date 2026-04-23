using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage queue list", "azure-storage/queue", "Lists queues in a storage account.")]
[CommandExample("List queues", "topaz storage queue list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\"")]
public sealed class ListQueuesCommand(ITopazLogger logger) : Command<ListQueuesCommand.ListQueuesCommandSettings>
{
    public override int Execute(CommandContext context, ListQueuesCommandSettings settings)
    {
        logger.LogInformation("Listing queues...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var controlPlane = new QueueServiceControlPlane(new QueueResourceProvider(logger), logger);
        var queuesOp = controlPlane.ListQueues(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!);
        var queues = queuesOp.Resource ?? [];

        if (queues.Length == 0)
        {
            logger.LogInformation("No queues found.");
            return 0;
        }

        foreach (var queue in queues)
            logger.LogInformation(queue.Name ?? "(unnamed)");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListQueuesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListQueuesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
