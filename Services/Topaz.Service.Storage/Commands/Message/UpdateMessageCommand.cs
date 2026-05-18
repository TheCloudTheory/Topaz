using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message update", "azure-storage/queue", "Updates the visibility timeout and/or content of a dequeued message.")]
[CommandExample("Update message visibility", "topaz storage message update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --message-id \"<id>\" \\\n    --pop-receipt \"<receipt>\" \\\n    --visibility-timeout 60")]
public sealed class UpdateMessageCommand(HttpClient httpClient) : TopazHttpCommand<UpdateMessageCommand.UpdateMessageCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateMessageCommandSettings settings)
    {
        var popEncoded = Uri.EscapeDataString(settings.PopReceipt!);
        var url = $"{QueueDataPlaneUrl(settings.AccountName!)}/{settings.QueueName}/messages/{settings.MessageId}?popreceipt={popEncoded}&visibilitytimeout={settings.VisibilityTimeout}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(settings.Content ?? string.Empty));
        var xml = $"<QueueMessage><MessageText>{encoded}</MessageText></QueueMessage>";
        using var content = new StringContent(xml, System.Text.Encoding.UTF8, "application/xml");
        var response = await HttpClient.PutAsync(url, content);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine("Message updated.");
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
