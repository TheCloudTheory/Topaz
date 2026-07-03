using System.Net.Http.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Disk.Commands;

[UsedImplicitly]
[CommandDefinition("disk grant-access", "managed-disk", "Grants SAS access to an Azure Managed Disk.")]
[CommandExample("Grant read access to a Managed Disk",
    "topaz disk grant-access --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-disk\" \\\n    --resource-group \"rg-local\" \\\n    --access \"Read\" \\\n    --duration-in-seconds 3600")]
internal sealed class GrantDiskAccessCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GrantDiskAccessCommand.GrantDiskAccessCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GrantDiskAccessCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Compute/disks/{settings.Name}/beginGetAccess?api-version=2023-04-02";
        using var content = JsonContent.Create(new
        {
            access = settings.Access,
            durationInSeconds = settings.DurationInSeconds
        });
        var response = await HttpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            AnsiConsole.WriteLine(await response.Content.ReadAsStringAsync());
            return 0;
        }

        // LRO: poll Azure-AsyncOperation until Succeeded
        var pollingUrl = response.Headers.TryGetValues("Azure-AsyncOperation", out var values)
            ? values.FirstOrDefault()
            : null;

        if (pollingUrl == null)
        {
            await Console.Error.WriteLineAsync("Error: missing Azure-AsyncOperation header.");
            return 1;
        }

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var poll = await HttpClient.GetAsync(pollingUrl);
            if (!poll.IsSuccessStatusCode)
            {
                await Console.Error.WriteLineAsync($"Polling error {(int)poll.StatusCode}: {await poll.Content.ReadAsStringAsync()}");
                return 1;
            }

            var status = await poll.Content.ReadFromJsonAsync<OperationStatus>();
            if (status?.Status == "Succeeded")
            {
                AnsiConsole.WriteLine(status.Properties?.Output?.AccessSAS ?? string.Empty);
                return 0;
            }

            if (status?.Status is "Failed" or "Canceled")
            {
                await Console.Error.WriteLineAsync($"Operation {status.Status}.");
                return 1;
            }
        }
    }

    protected override ValidationResult Validate(CommandContext context, GrantDiskAccessCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Disk name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Access))
            return ValidationResult.Error("Access level can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GrantDiskAccessCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Disk name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Access level: Read, Write, or ReadWrite.", required: true)]
        [CommandOption("--access")]
        public string? Access { get; set; }

        [CommandOptionDefinition("(Required) Duration of SAS access in seconds.", required: true)]
        [CommandOption("--duration-in-seconds")]
        public int DurationInSeconds { get; set; } = 3600;
    }

    private sealed class OperationStatus
    {
        public string? Status { get; init; }
        public OperationStatusProperties? Properties { get; init; }
    }

    private sealed class OperationStatusProperties
    {
        public OperationStatusOutput? Output { get; init; }
    }

    private sealed class OperationStatusOutput
    {
        public string? AccessSAS { get; init; }
    }
}
