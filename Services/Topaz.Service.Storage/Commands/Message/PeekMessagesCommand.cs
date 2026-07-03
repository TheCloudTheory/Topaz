using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message peek", "azure-storage/queue", "Peeks at one or more messages in a queue without dequeuing them.")]
[CommandExample("Peek at up to 5 messages", "topaz storage message peek \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --num-messages 5")]
public sealed class PeekMessagesCommand(HttpClient httpClient) : TopazHttpCommand<PeekMessagesCommand.PeekMessagesCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, PeekMessagesCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{QueueDataPlaneUrl(settings.AccountName!)}/{settings.QueueName}/messages?peekonly=true&numofmessages={settings.NumMessages}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, PeekMessagesCommandSettings settings)
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
    public sealed class PeekMessagesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-q|--queue-name")] public string? QueueName { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("Number of messages to peek (1–32). Default: 1.")]
        [CommandOption("-n|--num-messages")] public int NumMessages { get; set; } = 1;
    }
}
