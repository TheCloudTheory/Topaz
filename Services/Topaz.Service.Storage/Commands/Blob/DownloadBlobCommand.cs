using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob download", "azure-storage/blob", "Downloads a blob to a local file.")]
[CommandExample("Download a blob", "topaz storage blob download \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\" \\\n    --destination \"/tmp/file.txt\"")]
public sealed class DownloadBlobCommand(HttpClient httpClient) : TopazHttpCommand<DownloadBlobCommand.DownloadBlobCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DownloadBlobCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{settings.BlobName}";
        var response = await HttpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var destination = settings.Destination ?? settings.BlobName!;
        await File.WriteAllBytesAsync(destination, bytes);
        AnsiConsole.WriteLine($"Blob downloaded to: {destination}");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DownloadBlobCommandSettings settings)
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
    public sealed class DownloadBlobCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("Destination file path (defaults to blob name).")]
        [CommandOption("-d|--destination")] public string? Destination { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
