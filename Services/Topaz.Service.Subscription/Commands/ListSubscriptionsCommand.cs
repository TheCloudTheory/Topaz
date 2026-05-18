using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription list", "subscription", "Lists all subscriptions.")]
[CommandExample("List all subscriptions", "topaz subscription list")]
public sealed class ListSubscriptionsCommand(HttpClient httpClient) : TopazHttpCommand<ListSubscriptionsCommand.ListSubscriptionsCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListSubscriptionsCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    [UsedImplicitly]
    public sealed class ListSubscriptionsCommandSettings : CommandSettings { }
}