using Azure.Core;
using Azure.ResourceManager.Authorization;
using Topaz.Portal.Models.Rbac;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListRoleDefinitionsResponse?> ListRoleDefinitions(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"));

        var filter = string.IsNullOrWhiteSpace(roleNameFilter)
            ? null
            : $"roleName eq '{roleNameFilter.Replace("'", string.Empty)}'";

        var pageable = subscription.GetAuthorizationRoleDefinitions().GetAllAsync(
            filter: filter,
            cancellationToken: cancellationToken);

        await using var pageEnumerator = pageable
            .AsPages(continuationToken, pageSizeHint: pageSize)
            .GetAsyncEnumerator(cancellationToken);

        if (!await pageEnumerator.MoveNextAsync())
            return new ListRoleDefinitionsResponse { Value = [], ContinuationToken = null };

        var page = pageEnumerator.Current;

        var items = page.Values.Select(rd => new RoleDefinitionDto
        {
            Id = rd.Id.ToString(),
            Name = rd.Data.Name,
            Type = rd.Data.ResourceType.ToString(),
            Properties = new RoleDefinitionPropertiesDto
            {
                RoleName = rd.Data.RoleName,
                Description = rd.Data.Description,
                RoleType = rd.Data.RoleType.ToString()
            }
        }).ToArray();

        return new ListRoleDefinitionsResponse
        {
            Value = items,
            ContinuationToken = page.ContinuationToken
        };
    }

    public async Task<ListRoleAssignmentsResponse?> ListRoleAssignments(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"));

        var filter = string.IsNullOrWhiteSpace(roleNameFilter)
            ? null
            : $"roleName eq '{roleNameFilter.Replace("'", string.Empty)}'";

        var pageable = subscription.GetRoleAssignments().GetAllAsync(
            filter: filter,
            cancellationToken: cancellationToken);

        await using var pageEnumerator = pageable
            .AsPages(continuationToken, pageSizeHint: pageSize)
            .GetAsyncEnumerator(cancellationToken);

        if (!await pageEnumerator.MoveNextAsync())
            return new ListRoleAssignmentsResponse { Value = [], ContinuationToken = null };

        var page = pageEnumerator.Current;

        var items = page.Values.Select(ra => new RoleAssignmentDto
        {
            Id = ra.Id.ToString(),
            Name = ra.Data.Name,
            Type = ra.Data.ResourceType.ToString(),
            Properties = new RoleAssignmentPropertiesDto
            {
                RoleDefinitionId = ra.Data.RoleDefinitionId?.ToString(),
                PrincipalId = ra.Data.PrincipalId?.ToString(),
                PrincipalType = ra.Data.PrincipalType?.ToString(),
                Scope = ra.Data.Scope?.ToString()
            }
        }).ToArray();

        return new ListRoleAssignmentsResponse
        {
            Value = items,
            ContinuationToken = page.ContinuationToken
        };
    }
}
