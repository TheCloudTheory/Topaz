using Azure;
using Azure.Core;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.ManagedServiceIdentities.Models;
using Topaz.Portal.Models.ManagedIdentities;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListManagedIdentitiesResponse> ListManagedIdentities(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var identities = new List<ManagedIdentityDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var identity in subscriptionResource.GetUserAssignedIdentitiesAsync(cancellationToken: cancellationToken))
            {
                identities.Add(MapToManagedIdentityDto(identity, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListManagedIdentitiesResponse { Value = identities.ToArray() };
    }

    public async Task<ManagedIdentityDto?> GetManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}");

        var identity = await _armClient!.GetUserAssignedIdentityResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToManagedIdentityDto(identity.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string location,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var content = new UserAssignedIdentityData(new AzureLocation(location));

        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(
            WaitUntil.Completed,
            identityName,
            content,
            cancellationToken);
    }

    public async Task DeleteManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}");

        await _armClient!.GetUserAssignedIdentityResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateManagedIdentityTag(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetManagedIdentity(subscriptionId, resourceGroupName, identityName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating managed identity tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteManagedIdentityTag(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetManagedIdentity(subscriptionId, resourceGroupName, identityName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting managed identity tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListFederatedCredentialsResponse> ListFederatedCredentials(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}");

        var identity = _armClient!.GetUserAssignedIdentityResource(resourceId);
        var credentials = new List<FederatedCredentialDto>();

        await foreach (var cred in identity.GetFederatedIdentityCredentials().GetAllAsync(cancellationToken: cancellationToken))
        {
            credentials.Add(new FederatedCredentialDto
            {
                Id = cred.Id?.ToString(),
                Name = cred.Data.Name,
                Issuer = cred.Data.Issuer,
                Subject = cred.Data.Subject,
                Audiences = cred.Data.Audiences?.ToList() ?? []
            });
        }

        return new ListFederatedCredentialsResponse { Value = credentials.ToArray() };
    }

    public async Task CreateFederatedCredential(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string credentialName,
        string issuer,
        string subject,
        IList<string> audiences,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));
        if (string.IsNullOrWhiteSpace(credentialName))
            throw new ArgumentException("Credential name is required.", nameof(credentialName));
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer is required.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}");

        var identity = _armClient!.GetUserAssignedIdentityResource(resourceId);

        var data = new FederatedIdentityCredentialData
        {
            Issuer = issuer,
            Subject = subject,
        };
        foreach (var audience in audiences)
            data.Audiences.Add(audience);

        await identity.GetFederatedIdentityCredentials().CreateOrUpdateAsync(
            WaitUntil.Completed,
            credentialName,
            data,
            cancellationToken);
    }

    public async Task DeleteFederatedCredential(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string credentialName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(identityName))
            throw new ArgumentException("Identity name is required.", nameof(identityName));
        if (string.IsNullOrWhiteSpace(credentialName))
            throw new ArgumentException("Credential name is required.", nameof(credentialName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}/federatedIdentityCredentials/{credentialName}");

        await _armClient!.GetFederatedIdentityCredentialResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    private static ManagedIdentityDto MapToManagedIdentityDto(
        UserAssignedIdentityResource identity,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new ManagedIdentityDto
        {
            Id = identity.Id?.ToString(),
            Name = identity.Data.Name,
            Location = identity.Data.Location,
            ResourceGroupName = resourceGroupName ?? identity.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            ClientId = identity.Data.ClientId?.ToString(),
            PrincipalId = identity.Data.PrincipalId?.ToString(),
            TenantId = identity.Data.TenantId?.ToString(),
            ProvisioningState = null,
            Tags = identity.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(identity.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
}
