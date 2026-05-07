using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Portal.Models.ManagementGroups;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<GetManagementGroupEntitiesResponse> GetManagementGroupEntities(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        using var resp = await _httpClient.GetAsync(
            "/providers/Microsoft.Management/getEntities?api-version=2023-04-01",
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"GetManagementGroupEntities failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {errorBody}");
        }

        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(json);
        var valueArray = root?["value"]?.AsArray() ?? [];

        var dtos = new List<ManagementGroupEntityDto>();
        foreach (var item in valueArray)
        {
            if (item is null) continue;
            var id = item["id"]?.GetValue<string>() ?? string.Empty;
            var type = item["type"]?.GetValue<string>() ?? string.Empty;
            var name = item["name"]?.GetValue<string>() ?? string.Empty;
            var props = item["properties"];
            var displayName = props?["displayName"]?.GetValue<string>() ?? name;
            var parentId = props?["parent"]?["id"]?.GetValue<string>();

            dtos.Add(new ManagementGroupEntityDto
            {
                Id = id,
                Type = type,
                Name = name,
                DisplayName = displayName,
                ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId
            });
        }

        return new GetManagementGroupEntitiesResponse { Value = [.. dtos] };
    }

    public async Task CreateManagementGroup(
        string groupId,
        string displayName,
        string? parentGroupId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group ID is required.", nameof(groupId));

        object? details = parentGroupId is not null
            ? new
            {
                Parent = new
                {
                    Id = $"/providers/Microsoft.Management/managementGroups/{parentGroupId}"
                }
            }
            : null;

        var payload = new
        {
            Properties = new
            {
                DisplayName = displayName,
                Details = details
            }
        };

        using var resp = await _httpClient.PutAsJsonAsync(
            $"/providers/Microsoft.Management/managementGroups/{groupId}?api-version=2023-04-01",
            payload,
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"CreateManagementGroup failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task AssociateSubscriptionWithManagementGroup(
        string groupId,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group ID is required.", nameof(groupId));

        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        using var resp = await _httpClient.PutAsJsonAsync(
            $"/providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}?api-version=2023-04-01",
            new { },
            cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"AssociateSubscriptionWithManagementGroup failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }
}
