using Topaz.Portal.Models.KeyVaults;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<IReadOnlyList<KeyVaultSecretDto>> ListKeyVaultSecrets(
        string vaultUri,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));

        using var client = CreateVaultClient();

        var url = $"{vaultUri.TrimEnd('/')}/secrets?api-version=7.4";
        using var resp = await client.GetAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
            return [];

        var body = await resp.Content.ReadFromJsonAsync<SecretsListResponse>(cancellationToken: cancellationToken);

        return body?.Value?.Select(s => new KeyVaultSecretDto
        {
            Id = s.Id,
            Name = s.Id?.Split('/').LastOrDefault(),
            ContentType = s.ContentType,
            Enabled = s.Attributes?.Enabled ?? false,
            Created = s.Attributes?.Created is long c ? DateTimeOffset.FromUnixTimeSeconds(c) : null,
            Updated = s.Attributes?.Updated is long u ? DateTimeOffset.FromUnixTimeSeconds(u) : null,
        }).ToList() ?? [];
    }

    public async Task SetKeyVaultSecret(
        string vaultUri,
        string name,
        string value,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name is required.", nameof(name));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/secrets/{name}?api-version=7.4";
        var payload = new { value, contentType };
        using var resp = await client.PutAsJsonAsync(url, payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Setting secret failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name is required.", nameof(name));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/secrets/{name}?api-version=7.4";
        using var resp = await client.DeleteAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting secret failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<IReadOnlyList<KeyVaultDeletedSecretDto>> ListDeletedKeyVaultSecrets(
        string vaultUri,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/deletedsecrets?api-version=7.4";
        using var resp = await client.GetAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
            return [];

        var body = await resp.Content.ReadFromJsonAsync<DeletedSecretsListResponse>(cancellationToken: cancellationToken);
        return body?.Value?.Select(s => new KeyVaultDeletedSecretDto
        {
            Name = s.RecoveryId?.Split('/').LastOrDefault() ?? s.Id?.Split('/').LastOrDefault(),
            ContentType = s.ContentType,
            Enabled = s.Attributes?.Enabled ?? false,
            DeletedDate = s.DeletedDate != 0 ? DateTimeOffset.FromUnixTimeSeconds(s.DeletedDate) : null,
            ScheduledPurgeDate = s.ScheduledPurgeDate != 0 ? DateTimeOffset.FromUnixTimeSeconds(s.ScheduledPurgeDate) : null,
        }).ToList() ?? [];
    }

    public async Task<string> BackupKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name is required.", nameof(name));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/secrets/{name}/backup?api-version=7.4";
        using var resp = await client.PostAsync(url, null, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Backup failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }

        var result = await resp.Content.ReadFromJsonAsync<BackupSecretBlobResponse>(cancellationToken: cancellationToken);
        return result?.Value ?? string.Empty;
    }

    public async Task RestoreKeyVaultSecret(
        string vaultUri,
        string backupBlob,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(backupBlob))
            throw new ArgumentException("Backup blob is required.", nameof(backupBlob));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/secrets/restore?api-version=7.4";
        var payload = new { value = backupBlob };
        using var resp = await client.PostAsJsonAsync(url, payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Restore failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task RecoverDeletedKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name is required.", nameof(name));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/deletedsecrets/{name}/recover?api-version=7.4";
        using var resp = await client.PostAsync(url, null, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Recover failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task PurgeDeletedKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(vaultUri))
            throw new ArgumentException("Vault URI is required.", nameof(vaultUri));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name is required.", nameof(name));

        using var client = CreateVaultClient();
        var url = $"{vaultUri.TrimEnd('/')}/deletedsecrets/{name}?api-version=7.4";
        using var resp = await client.DeleteAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Purge failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    private HttpClient CreateVaultClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Token);
        return client;
    }

    private sealed class SecretsListResponse
    {
        public SecretsListItem[]? Value { get; init; }
    }

    private sealed class SecretsListItem
    {
        public string? Id { get; init; }
        public string? ContentType { get; init; }
        public SecretsListAttributes? Attributes { get; init; }
    }

    private sealed class SecretsListAttributes
    {
        public bool Enabled { get; init; }
        public long Created { get; init; }
        public long Updated { get; init; }
    }

    private sealed class DeletedSecretsListResponse
    {
        public DeletedSecretListItem[]? Value { get; init; }
    }

    private sealed class DeletedSecretListItem
    {
        public string? RecoveryId { get; init; }
        public string? Id { get; init; }
        public string? ContentType { get; init; }
        public long DeletedDate { get; init; }
        public long ScheduledPurgeDate { get; init; }
        public DeletedSecretListAttributes? Attributes { get; init; }
    }

    private sealed class DeletedSecretListAttributes
    {
        public bool Enabled { get; init; }
    }

    private sealed class BackupSecretBlobResponse
    {
        public string? Value { get; init; }
    }
}
