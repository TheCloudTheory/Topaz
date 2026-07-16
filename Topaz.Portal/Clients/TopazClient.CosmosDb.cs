using Azure;
using Azure.Core;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Topaz.Portal.Models.CosmosDb;
using Topaz.ResourceManager;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListCosmosDbAccountsResponse> ListCosmosDbAccounts(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var accounts = new List<CosmosDbAccountDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var account in subscriptionResource.GetCosmosDBAccountsAsync(cancellationToken: cancellationToken))
            {
                accounts.Add(MapToCosmosDbAccountDto(account, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListCosmosDbAccountsResponse { Value = accounts.ToArray() };
    }

    public async Task<CosmosDbAccountDto?> GetCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));

        var resourceId = CosmosDBAccountResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, accountName);
        var account = await _armClient!.GetCosmosDBAccountResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToCosmosDbAccountDto(account.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string location,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var accountData = new CosmosDBAccountCreateOrUpdateContent(
            new AzureLocation(location),
            [new CosmosDBAccountLocation { LocationName = new AzureLocation(location) }]);

        await rg.Value.GetCosmosDBAccounts().CreateOrUpdateAsync(
            WaitUntil.Completed,
            accountName,
            accountData,
            cancellationToken);
    }

    public async Task DeleteCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));

        var resourceId = CosmosDBAccountResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, accountName);
        await _armClient!.GetCosmosDBAccountResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateCosmosDbAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var existing = await GetCosmosDbAccount(subscriptionId, resourceGroupName, accountName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}",
            payload,
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating Cosmos DB account tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteCosmosDbAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetCosmosDbAccount(subscriptionId, resourceGroupName, accountName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}",
            payload,
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting Cosmos DB account tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<CosmosDbAccountKeysDto> GetCosmosDbAccountKeys(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = CosmosDBAccountResource.CreateResourceIdentifier(subscriptionId.ToString(), resourceGroupName, accountName);
        var keys = await _armClient!.GetCosmosDBAccountResource(resourceId).GetKeysAsync(cancellationToken: cancellationToken);

        var primaryKey = keys.Value.PrimaryMasterKey ?? string.Empty;
        var secondaryKey = keys.Value.SecondaryMasterKey ?? string.Empty;

        return new CosmosDbAccountKeysDto
        {
            PrimaryConnectionString = TopazResourceHelpers.GetCosmosDbConnectionString(accountName, primaryKey),
            SecondaryConnectionString = TopazResourceHelpers.GetCosmosDbConnectionString(accountName, secondaryKey),
            PrimaryKey = primaryKey,
            SecondaryKey = secondaryKey,
            AccountEndpoint = TopazResourceHelpers.GetCosmosDbAccountEndpoint(accountName),
        };
    }

    private static CosmosDbAccountDto MapToCosmosDbAccountDto(
        CosmosDBAccountResource account,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new CosmosDbAccountDto
        {
            Id = account.Id?.ToString(),
            Name = account.Data.Name,
            Location = account.Data.Location.ToString(),
            ResourceGroupName = resourceGroupName ?? account.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            Kind = account.Data.Kind?.ToString(),
            DocumentEndpoint = account.Data.DocumentEndpoint,
            ProvisioningState = account.Data.ProvisioningState,
            Tags = account.Data.Tags is not null
                ? new Dictionary<string, string>(account.Data.Tags)
                : [],
        };
    }
}
