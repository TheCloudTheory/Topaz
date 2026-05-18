using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob update", "azure-storage/blob", "Updates the HTTP properties (content type, encoding, etc.) of a blob.")]
[CommandExample("Update content type of a blob", "topaz storage blob update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\" \\\n    --content-type \"text/plain\"")]
public sealed class SetBlobPropertiesCommand(HttpClient httpClient) : TopazHttpCommand<SetBlobPropertiesCommand.SetBlobPropertiesCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetBlobPropertiesCommandSettings settings)
    {
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{settings.BlobName}?comp=properties";
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (!string.IsNullOrEmpty(settings.ContentType))
            request.Headers.TryAddWithoutValidation("x-ms-blob-content-type", settings.ContentType);
        if (!string.IsNullOrEmpty(settings.ContentEncoding))
            request.Headers.TryAddWithoutValidation("x-ms-blob-content-encoding", settings.ContentEncoding);
        if (!string.IsNullOrEmpty(settings.ContentLanguage))
            request.Headers.TryAddWithoutValidation("x-ms-blob-content-language", settings.ContentLanguage);
        if (!string.IsNullOrEmpty(settings.CacheControl))
            request.Headers.TryAddWithoutValidation("x-ms-blob-cache-control", settings.CacheControl);
        if (!string.IsNullOrEmpty(settings.ContentDisposition))
            request.Headers.TryAddWithoutValidation("x-ms-blob-content-disposition", settings.ContentDisposition);
        request.Content = new StringContent(string.Empty);
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine("Properties set.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, SetBlobPropertiesCommandSettings settings)
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
    public sealed class SetBlobPropertiesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("Content-Type header value (e.g. text/plain).")]
        [CommandOption("--content-type")] public string? ContentType { get; set; }
        [CommandOptionDefinition("Content-Encoding header value.")]
        [CommandOption("--content-encoding")] public string? ContentEncoding { get; set; }
        [CommandOptionDefinition("Content-Language header value.")]
        [CommandOption("--content-language")] public string? ContentLanguage { get; set; }
        [CommandOptionDefinition("Cache-Control header value.")]
        [CommandOption("--cache-control")] public string? CacheControl { get; set; }
        [CommandOptionDefinition("Content-Disposition header value.")]
        [CommandOption("--content-disposition")] public string? ContentDisposition { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
