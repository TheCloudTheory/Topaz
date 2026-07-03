using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob lease", "azure-storage/blob", "Manages lease operations on a blob (acquire, renew, change, release, break).")]
[CommandExample("Acquire a lease", "topaz storage blob lease \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\" \\\n    --action \"acquire\" \\\n    --lease-duration 60")]
public sealed class LeaseBlobCommand(HttpClient httpClient) : TopazHttpCommand<LeaseBlobCommand.LeaseBlobCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, LeaseBlobCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{settings.BlobName}?comp=lease";
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Add("x-ms-lease-action", settings.Action!.ToLower());
        if (!string.IsNullOrEmpty(settings.LeaseId))
            request.Headers.Add("x-ms-lease-id", settings.LeaseId);
        if (settings.LeaseDuration.HasValue)
            request.Headers.Add("x-ms-lease-duration", settings.LeaseDuration.Value.ToString());
        if (!string.IsNullOrEmpty(settings.ProposedLeaseId))
            request.Headers.Add("x-ms-proposed-lease-id", settings.ProposedLeaseId);
        if (settings.LeaseBreakPeriod.HasValue)
            request.Headers.Add("x-ms-lease-break-period", settings.LeaseBreakPeriod.Value.ToString());
        request.Content = new StringContent(string.Empty);
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine(await response.Content.ReadAsStringAsync());
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, LeaseBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.BlobName))
            return ValidationResult.Error("Blob name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Action))
            return ValidationResult.Error("Lease action can't be null.");

        var valid = new[] { "acquire", "renew", "change", "release", "break" };
        if (!valid.Contains(settings.Action.ToLowerInvariant()))
            return ValidationResult.Error($"Invalid lease action '{settings.Action}'. Must be one of: {string.Join(", ", valid)}.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class LeaseBlobCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("(Required) Lease action: acquire, renew, change, release, or break.", required: true)]
        [CommandOption("--action")] public string? Action { get; set; }
        [CommandOptionDefinition("Lease duration in seconds (-1 for infinite).")]
        [CommandOption("--lease-duration")] public int? LeaseDuration { get; set; }
        [CommandOptionDefinition("Existing lease ID (required for renew, change, release).")]
        [CommandOption("--lease-id")] public string? LeaseId { get; set; }
        [CommandOptionDefinition("Proposed lease ID (required for change action).")]
        [CommandOption("--proposed-lease-id")] public string? ProposedLeaseId { get; set; }
        [CommandOptionDefinition("Break period in seconds (used with break action).")]
        [CommandOption("--lease-break-period")] public int? LeaseBreakPeriod { get; set; }
    }
}
