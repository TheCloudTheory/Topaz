using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.CLI.Commands;

[CommandDefinition("seed", "generic", "Imports resources from a remote source.")]
internal sealed class SeedCommand(HttpClient httpClient)
    : TopazHttpCommand<SeedCommand.SeedCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SeedCommandSettings settings, CancellationToken cancellationToken)
    {
        var request = new
        {
            settings.SubscriptionId,
            settings.ResourceGroup,
            settings.ResourceType,
            settings.DryRun,
            settings.Overwrite
        };

        var response = await HttpClient.PostAsync(
            $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/topaz/extras/seed",
            new StringContent(JsonSerializer.Serialize(request, GlobalSettings.JsonOptions)), cancellationToken);
        
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
            return 1;
        }
        
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, SeedCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("SubscriptionId is required");
        }
        
        return Guid.TryParse(settings.SubscriptionId, out _) ? ValidationResult.Error("SubscriptionId is not a valid GUID") : ValidationResult.Success();
    }
    
    [UsedImplicitly]
    internal sealed class SeedCommandSettings : CommandSettings
    {
        public string? SubscriptionId { get; set; }
        public string? ResourceGroup { get; set; }
        public string? ResourceType { get; set; }
        public bool DryRun { get; set; }
        public bool Overwrite { get; set; }
    }
}