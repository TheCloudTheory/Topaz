using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models;

internal sealed class ManagementGroupSubscription
{
    public string Id { get; init; } = string.Empty;

    public string Type => "Microsoft.Management/managementGroups/subscriptions";

    public string Name { get; init; } = string.Empty;

    public ManagementGroupSubscriptionProperties Properties { get; init; } = new();

    public static ManagementGroupSubscription Create(string groupId, string subscriptionId, string displayName)
    {
        return new ManagementGroupSubscription
        {
            Id = $"/providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}",
            Name = subscriptionId,
            Properties = new ManagementGroupSubscriptionProperties
            {
                Tenant = GlobalSettings.DefaultTenantId,
                DisplayName = displayName,
                Parent = new ManagementGroupSubscriptionParent
                {
                    Id = $"/providers/Microsoft.Management/managementGroups/{groupId}",
                    Name = groupId
                },
                State = "Active"
            }
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class ManagementGroupSubscriptionProperties
{
    public string Tenant { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ManagementGroupSubscriptionParent Parent { get; set; } = new();

    public string State { get; set; } = "Active";
}

internal sealed class ManagementGroupSubscriptionParent
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
