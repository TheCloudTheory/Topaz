using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob snapshot", "azure-storage/blob", "Creates a read-only snapshot of a blob at the current time.")]
[CommandExample("Create a blob snapshot", "topaz storage blob snapshot \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\"")]
public sealed class SnapshotBlobCommand(HttpClient httpClient) : TopazHttpCommand<SnapshotBlobCommand.SnapshotBlobCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SnapshotBlobCommandSettings settings)
    {
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{settings.BlobName}?comp=snapshot";
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (!string.IsNullOrEmpty(settings.LeaseId))
            request.Headers.Add("x-ms-lease-id", settings.LeaseId);
        request.Content = new StringContent(string.Empty);
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine("Snapshot created.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SnapshotBlobCommandSettings settings)
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
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class SnapshotBlobCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("Lease ID required if the blob has an active lease.")]
        [CommandOption("--lease-id")] public string? LeaseId { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
