using Azure.Core;
using Azure.ResourceManager.Resources;
using Topaz.Portal.Models.Subscriptions;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListSubscriptionsResponse> ListSubscriptions()
    {
        await EnsureInitializedAsync();

        var subscriptions = new List<SubscriptionResource>();

        await foreach (var subscription in _armClient!.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(subscription);
        }

        return new ListSubscriptionsResponse
        {
            Value = subscriptions.Select(sub => new SubscriptionDto
            {
                DisplayName = sub.Data.DisplayName,
                SubscriptionId = sub.Data.SubscriptionId,
                Id = sub.Id.ToString(),
                Tags = sub.Data.Tags is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(sub.Data.Tags, StringComparer.OrdinalIgnoreCase)
            }).ToArray()
        };
    }

    public async Task<SubscriptionDto?> GetSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);

        return new SubscriptionDto
        {
            DisplayName = subscription.Value.Data.DisplayName,
            SubscriptionId = subscription.Value.Data.SubscriptionId,
            Id = subscription.Value.Id.ToString(),
            Tags = subscription.Value.Data.Tags is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task UpdateSubscriptionDisplayName(
        Guid subscriptionId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);

        var payload = new
        {
            SubscriptionName = displayName,
            Tags = subscription.Value.Data.Tags is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };

        using var resp =
            await _httpClient.PatchAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating subscription display name failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateOrUpdateSubscriptionTag(
        Guid subscriptionId,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);

        var payload = new
        {
            SubscriptionName = subscription.Value.Data.DisplayName,
            Tags = subscription.Value.Data.Tags is null
                ? new Dictionary<string, string> { { tagName, tagValue } }
                : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase) { { tagName, tagValue } }
        };

        using var resp =
            await _httpClient.PatchAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating subscription failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteSubscriptionTag(
        Guid subscriptionId,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);

        var tags = subscription.Value.Data.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new
        {
            SubscriptionName = subscription.Value.Data.DisplayName,
            Tags = tags
        };

        using var resp =
            await _httpClient.PatchAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting subscription tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateSubscription(Guid subscriptionId, string subscriptionName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name is required.", nameof(subscriptionName));

        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("Topaz:ArmBaseUrl is not configured.");

        var payload = new
        {
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName
        };

        using var resp =
            await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Create subscription failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }
}
