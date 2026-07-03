using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob metadata update", "azure-storage/blob", "Sets or replaces metadata key-value pairs on a blob.")]
[CommandExample("Set blob metadata", "topaz storage blob metadata update \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\" \\\n    --metadata \"env=prod\" \"owner=team\"")]
public sealed class SetBlobMetadataCommand(HttpClient httpClient) : TopazHttpCommand<SetBlobMetadataCommand.SetBlobMetadataCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SetBlobMetadataCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{BlobDataPlaneUrl(settings.AccountName!)}/{settings.ContainerName}/{settings.BlobName}?comp=metadata";
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (settings.Metadata != null)
            foreach (var kv in settings.Metadata)
            {
                var parts = kv.Split('=', 2);
                if (parts.Length == 2) request.Headers.TryAddWithoutValidation($"x-ms-meta-{parts[0]}", parts[1]);
            }
        request.Content = new StringContent(string.Empty);
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return 1;
        }
        AnsiConsole.WriteLine("Metadata set.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, SetBlobMetadataCommandSettings settings)
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
    public sealed class SetBlobMetadataCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("Metadata key=value pairs (e.g. --metadata \"env=prod\" \"owner=team\").")]
        [CommandOption("--metadata")] public string[]? Metadata { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
