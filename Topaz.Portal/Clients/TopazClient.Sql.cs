using Azure;
using Azure.Core;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Topaz.Portal.Models.Sql;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListSqlServersResponse> ListSqlServers(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var servers = new List<SqlServerDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var server in subscriptionResource.GetSqlServersAsync(cancellationToken: cancellationToken))
            {
                servers.Add(MapToSqlServerDto(server, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListSqlServersResponse { Value = servers.ToArray() };
    }

    public async Task<SqlServerDto?> GetSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        var resourceId = SqlServerResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, serverName);
        var server = await _armClient!.GetSqlServerResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToSqlServerDto(server.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string location,
        string administratorLogin,
        string administratorLoginPassword,
        string version = "12.0",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        if (string.IsNullOrWhiteSpace(administratorLogin))
            throw new ArgumentException("Administrator login is required.", nameof(administratorLogin));

        if (string.IsNullOrWhiteSpace(administratorLoginPassword))
            throw new ArgumentException("Administrator login password is required.", nameof(administratorLoginPassword));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var serverData = new SqlServerData(new AzureLocation(location))
        {
            AdministratorLogin = administratorLogin,
            AdministratorLoginPassword = administratorLoginPassword,
            Version = version
        };

        await rg.Value.GetSqlServers().CreateOrUpdateAsync(
            WaitUntil.Completed,
            serverName,
            serverData,
            cancellationToken);
    }

    public async Task DeleteSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        var resourceId = SqlServerResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, serverName);
        await _armClient!.GetSqlServerResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateSqlServerTag(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var existing = await GetSqlServer(subscriptionId, resourceGroupName, serverName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}",
            payload,
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating SQL server tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteSqlServerTag(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetSqlServer(subscriptionId, resourceGroupName, serverName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}",
            payload,
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting SQL server tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListSqlDatabasesResponse> ListSqlDatabases(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var serverId = SqlServerResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, serverName);
        var server = _armClient!.GetSqlServerResource(serverId);
        var databases = new List<SqlDatabaseDto>();

        await foreach (var database in server.GetSqlDatabases().GetAllAsync(cancellationToken: cancellationToken))
        {
            databases.Add(MapToSqlDatabaseDto(database, serverName));
        }

        return new ListSqlDatabasesResponse { Value = databases.ToArray() };
    }

    public async Task<SqlDatabaseDto?> GetSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("SQL database name is required.", nameof(databaseName));

        var resourceId = SqlDatabaseResource.CreateResourceIdentifier(
            subscriptionId.ToString(),
            resourceGroupName,
            serverName,
            databaseName);

        var database = await _armClient!.GetSqlDatabaseResource(resourceId).GetAsync(cancellationToken: cancellationToken);
        return MapToSqlDatabaseDto(database.Value, serverName);
    }

    public async Task CreateSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("SQL database name is required.", nameof(databaseName));

        var server = await GetSqlServer(subscriptionId, resourceGroupName, serverName, cancellationToken);
        var location = string.IsNullOrWhiteSpace(server?.Location) ? "westeurope" : server.Location;

        var serverId = SqlServerResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, serverName);
        await _armClient!
            .GetSqlServerResource(serverId)
            .GetSqlDatabases()
            .CreateOrUpdateAsync(
                WaitUntil.Completed,
                databaseName,
                new SqlDatabaseData(new AzureLocation(location)),
                cancellationToken);
    }

    public async Task DeleteSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("SQL server name is required.", nameof(serverName));

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("SQL database name is required.", nameof(databaseName));

        var resourceId = SqlDatabaseResource.CreateResourceIdentifier(
            subscriptionId.ToString(),
            resourceGroupName,
            serverName,
            databaseName);

        await _armClient!.GetSqlDatabaseResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    private static SqlServerDto MapToSqlServerDto(
        SqlServerResource server,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new SqlServerDto
        {
            Id = server.Id?.ToString(),
            Name = server.Data.Name,
            Location = server.Data.Location.ToString(),
            ResourceGroupName = resourceGroupName ?? server.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            FullyQualifiedDomainName = server.Data.FullyQualifiedDomainName,
            Version = server.Data.Version,
            State = server.Data.State,
            AdministratorLogin = server.Data.AdministratorLogin,
            Tags = server.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(server.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static SqlDatabaseDto MapToSqlDatabaseDto(SqlDatabaseResource database, string serverName)
    {
        return new SqlDatabaseDto
        {
            Id = database.Id?.ToString(),
            Name = database.Data.Name,
            ServerName = serverName,
            Status = database.Data.Status?.ToString(),
            Location = database.Data.Location.ToString(),
            Collation = database.Data.Collation,
            MaxSizeBytes = database.Data.MaxSizeBytes,
            CurrentServiceObjectiveName = database.Data.CurrentServiceObjectiveName,
            Tags = database.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(database.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
}
