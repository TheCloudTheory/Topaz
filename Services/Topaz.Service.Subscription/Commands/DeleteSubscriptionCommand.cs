using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription delete", "subscription", "Deletes a subscription.")]
[CommandExample("Delete a subscription", "topaz subscription delete \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public class DeleteSubscriptionCommand(HttpClient httpClient) : TopazHttpCommand<DeleteSubscriptionCommand.DeleteSubscriptionCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.Id}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Subscription '{settings.Id}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteSubscriptionCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.Id) ? ValidationResult.Error("Subscription ID can't be null.") : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")]
        public string? Id { get; set; }
    }
}
