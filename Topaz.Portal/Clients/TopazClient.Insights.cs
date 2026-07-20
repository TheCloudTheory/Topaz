using Azure;
using Azure.Core;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.ApplicationInsights.Models;
using AiData = Azure.ResourceManager.ApplicationInsights.ApplicationInsightsComponentData;
using Topaz.Portal.Models.Insights;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListApplicationInsightsResponse> ListApplicationInsights()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var components = new List<ApplicationInsightsDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var component in subscriptionResource.GetApplicationInsightsComponentsAsync())
            {
                components.Add(MapToDto(component.Data, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListApplicationInsightsResponse { Value = components.ToArray() };
    }

    public async Task<ApplicationInsightsDto?> GetApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}");

        var component = await _armClient!.GetApplicationInsightsComponentResource(resourceId).GetAsync(cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToDto(component.Value.Data, subscriptionId.ToString(), subscription.Value.Data.DisplayName);
    }

    public async Task CreateApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string location,
        string kind = "web",
        string applicationType = "web",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var content = new AiData(new AzureLocation(location), kind)
        {
            ApplicationType = new ApplicationInsightsApplicationType(applicationType)
        };

        _ = await rg.Value.GetApplicationInsightsComponents().CreateOrUpdateAsync(
            WaitUntil.Completed,
            componentName,
            content,
            cancellationToken);
    }

    public async Task DeleteApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}");

        _ = await _armClient!.GetApplicationInsightsComponentResource(resourceId).DeleteAsync(
            WaitUntil.Completed,
            cancellationToken);
    }

    public async Task CreateOrUpdateApplicationInsightsTag(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetApplicationInsights(subscriptionId, resourceGroupName, componentName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating Application Insights tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteApplicationInsightsTag(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name is required.", nameof(componentName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetApplicationInsights(subscriptionId, resourceGroupName, componentName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting Application Insights tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ApplicationInsightsQueryResponse> QueryApplicationInsights(
        string connectionString,
        string query,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("InstrumentationKey", out var ikey) || string.IsNullOrWhiteSpace(ikey))
            throw new InvalidOperationException("InstrumentationKey not found in connection string.");
        if (!parts.TryGetValue("IngestionEndpoint", out var ingestionEndpoint) || string.IsNullOrWhiteSpace(ingestionEndpoint))
            throw new InvalidOperationException("IngestionEndpoint not found in connection string.");

        var url = $"{ingestionEndpoint.TrimEnd('/')}/v1/apps/{ikey}/query";
        var payload = new { query };

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Token);

        using var resp = await client.PostAsJsonAsync(url, payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Query failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }

        return await resp.Content.ReadFromJsonAsync<ApplicationInsightsQueryResponse>(cancellationToken: cancellationToken)
               ?? new ApplicationInsightsQueryResponse();
    }

    private static ApplicationInsightsDto MapToDto(
        AiData data,
        string? subscriptionId,
        string? subscriptionName)
        => new()
        {
            Id = data.Id?.ToString(),
            Name = data.Name,
            Location = data.Location,
            ResourceGroupName = data.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            Kind = data.Kind,
            ApplicationType = data.ApplicationType?.ToString(),
            InstrumentationKey = data.InstrumentationKey,
            ConnectionString = data.ConnectionString,
            IngestionMode = data.IngestionMode?.ToString(),
            RetentionInDays = data.RetentionInDays.GetValueOrDefault(90),
            Tags = data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(data.Tags, StringComparer.OrdinalIgnoreCase)
        };
}
