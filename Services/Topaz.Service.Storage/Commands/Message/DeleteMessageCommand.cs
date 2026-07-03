using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message delete", "azure-storage/queue", "Deletes a message from a queue.")]
[CommandExample("Delete a message", "topaz storage message delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --message-id \"<id>\" \\\n    --pop-receipt \"<receipt>\"")]
public sealed class DeleteMessageCommand(HttpClient httpClient) : TopazHttpCommand<DeleteMessageCommand.DeleteMessageCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteMessageCommandSettings settings, CancellationToken cancellationToken)
    {
        var popEncoded = Uri.EscapeDataString(settings.PopReceipt!);
        var url = $"{QueueDataPlaneUrl(settings.AccountName!)}/{settings.QueueName}/messages/{settings.MessageId}?popreceipt={popEncoded}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine("Message deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteMessageCommandSettings settings)
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
    public sealed class DeleteMessageCommandSettings : CommandSettings
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
    }
}
