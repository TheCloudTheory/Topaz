using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob upload", "azure-storage/blob", "Uploads a local file to a blob container.")]
[CommandExample("Upload a file", "topaz storage blob upload \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --file \"/path/to/file.txt\"")]
public sealed class UploadBlobCommand(HttpClient httpClient) : TopazHttpCommand<UploadBlobCommand.UploadBlobCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UploadBlobCommandSettings settings)
    {
        var blobName = settings.BlobName ?? Path.GetFileName(settings.FilePath)!;
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{blobName}";
        await using var stream = File.OpenRead(settings.FilePath!);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        var response = await HttpClient.PutAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine($"Blob uploaded: {blobName}");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UploadBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.FilePath))
            return ValidationResult.Error("File path can't be null.");
        if (!File.Exists(settings.FilePath))
            return ValidationResult.Error($"File '{settings.FilePath}' does not exist.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UploadBlobCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Path to the local file to upload.", required: true)]
        [CommandOption("-f|--file")] public string? FilePath { get; set; }
        [CommandOptionDefinition("Blob name (defaults to the file name).")]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
