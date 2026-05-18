using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Message;

[UsedImplicitly]
[CommandDefinition("storage message put", "azure-storage/queue", "Enqueues a new message in a queue.")]
[CommandExample("Enqueue a message", "topaz storage message put \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --queue-name \"myqueue\" \\\n    --content \"Hello World\"")]
public sealed class SendMessageCommand(HttpClient httpClient) : TopazHttpCommand<SendMessageCommand.SendMessageCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SendMessageCommandSettings settings)
    {
        var url = $"{QueueDataPlaneUrl(settings.AccountName!)}/{settings.QueueName}/messages";
        if (settings.VisibilityTimeout > 0)
            url += $"?visibilitytimeout={settings.VisibilityTimeout}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(settings.Content!));
        var xml = $"<QueueMessage><MessageText>{encoded}</MessageText></QueueMessage>";
        using var content = new StringContent(xml, System.Text.Encoding.UTF8, "application/xml");
        var response = await HttpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine(await response.Content.ReadAsStringAsync());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SendMessageCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.QueueName))
            return ValidationResult.Error("Queue name can't be null.");
        if (string.IsNullOrEmpty(settings.Content))
            return ValidationResult.Error("Message content can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class SendMessageCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Queue name.", required: true)]
        [CommandOption("-q|--queue-name")] public string? QueueName { get; set; }
        [CommandOptionDefinition("(Required) Message content.", required: true)]
        [CommandOption("-c|--content")] public string? Content { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("Seconds the message is invisible after enqueue (0–604800). Default: 0.")]
        [CommandOption("--visibility-timeout")] public int VisibilityTimeout { get; set; } = 0;
        [CommandOptionDefinition("Message time-to-live in seconds (1–604800). Default: 604800.")]
        [CommandOption("--ttl")] public int? Ttl { get; set; }
    }
}
