using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.FinOps.Commands;

[UsedImplicitly]
[CommandDefinition("finops estimate", "finops", "Estimate monthly costs for a subscription.")]
public sealed class EstimateCostsCommand(HttpClient httpClient)
    : TopazHttpCommand<EstimateCostsCommand.EstimateCostsCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, EstimateCostsCommandSettings settings, CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(settings.Currency) ? "USD" : settings.Currency;
        var url = $"{ArmBaseUrl}/topaz/subscriptions/{settings.SubscriptionId}/estimatedCosts?currency={currency}";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;

        if (settings.Output == OutputFormat.Json)
        {
            AnsiConsole.WriteLine(body);
            return 0;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var totalCost = root.TryGetProperty("totalMonthlyCost", out var totalEl)
            ? totalEl.GetDouble()
            : 0.0;

        var responseCurrency = root.TryGetProperty("currency", out var currencyEl)
            ? currencyEl.GetString() ?? currency
            : currency;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Resource Type[/]")
            .AddColumn(new TableColumn("[bold]Est. Monthly Cost[/]").RightAligned());

        if (root.TryGetProperty("resources", out var resourcesEl))
        {
            foreach (var resource in resourcesEl.EnumerateArray())
            {
                var resourceType = resource.TryGetProperty("resourceType", out var typeEl)
                    ? typeEl.GetString() ?? "-"
                    : "-";
                var cost = resource.TryGetProperty("estimatedMonthlyCost", out var costEl)
                    ? costEl.GetDouble()
                    : 0.0;

                table.AddRow(
                    Markup.Escape(resourceType),
                    $"{cost:N2} {responseCurrency}");
            }
        }

        table.AddEmptyRow();
        table.AddRow("[bold]Total[/]", $"[bold]{totalCost:N2} {responseCurrency}[/]");

        AnsiConsole.Write(table);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, EstimateCostsCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SubscriptionId))
        {
            return ValidationResult.Error("Subscription ID is required (--subscription).");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class EstimateCostsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID", required: true)]
        [CommandOption("-s|--subscription")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Currency code (default: USD)")]
        [CommandOption("-c|--currency")]
        public string Currency { get; set; } = "USD";

        [CommandOptionDefinition("Output format: table (default) or json")]
        [CommandOption("-o|--output")]
        public OutputFormat Output { get; set; } = OutputFormat.Table;
    }

    public enum OutputFormat { Table, Json }
}
