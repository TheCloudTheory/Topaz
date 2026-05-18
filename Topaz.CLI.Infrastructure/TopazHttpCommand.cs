using System.Net.Http.Json;
using Spectre.Console.Cli;
using Topaz.Shared;

namespace Topaz.CLI.Infrastructure;

/// <summary>
/// Base class for all Topaz CLI commands. Commands call the Topaz Host over HTTP/HTTPS
/// rather than invoking control-plane classes in-process, so they work regardless of
/// whether the CLI binary runs in the same process or container as the Host.
/// </summary>
public abstract class TopazHttpCommand<TSettings>(HttpClient httpClient) : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    protected readonly HttpClient HttpClient = httpClient;

    /// <summary>Base URL for all ARM / control-plane operations.</summary>
    protected string ArmBaseUrl =>
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}";

    /// <summary>Base URL for Key Vault data-plane operations.</summary>
    protected string KvDataPlaneUrl(string vaultName) =>
        $"https://{vaultName}.vault.topaz.local.dev:{GlobalSettings.DefaultKeyVaultPort}";

    /// <summary>Base URL for Blob storage data-plane operations.</summary>
    protected string BlobDataPlaneUrl(string accountName) =>
        $"https://{accountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}";

    /// <summary>Base URL for Queue storage data-plane operations.</summary>
    protected string QueueDataPlaneUrl(string accountName) =>
        $"https://{accountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}";

    /// <summary>Base URL for Table storage data-plane operations.</summary>
    protected string TableDataPlaneUrl(string accountName) =>
        $"https://{accountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}";

    /// <summary>Sends GET; returns (true, responseBody) on success or (false, body) on error.</summary>
    protected async Task<(bool Success, string Body)> GetAsync(
        string url, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return (true, body);
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
        return (false, body);
    }

    /// <summary>Sends PUT with a JSON body; returns (true, responseBody) on success.</summary>
    protected async Task<(bool Success, string Body)> PutAsync<TRequest>(
        string url, TRequest request, CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(request, options: GlobalSettings.JsonOptions);
        var response = await HttpClient.PutAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return (true, body);
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
        return (false, body);
    }

    /// <summary>Sends POST with a JSON body; returns (true, responseBody) on success.</summary>
    protected async Task<(bool Success, string Body)> PostAsync<TRequest>(
        string url, TRequest request, CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(request, options: GlobalSettings.JsonOptions);
        var response = await HttpClient.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return (true, body);
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
        return (false, body);
    }

    /// <summary>Sends PATCH with a JSON body; returns (true, responseBody) on success.</summary>
    protected async Task<(bool Success, string Body)> PatchAsync<TRequest>(
        string url, TRequest request, CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(request, options: GlobalSettings.JsonOptions);
        var response = await HttpClient.PatchAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return (true, body);
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
        return (false, body);
    }

    /// <summary>Sends DELETE; returns true on success (200/202/204/404 all treated as ok).</summary>
    protected async Task<bool> DeleteAsync(
        string url, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.DeleteAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound) return true;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
        return false;
    }
}
