using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiRevisionData
{
    public string? ApiId { get; init; }
    public string ApiRevision { get; set; } = "1";
    public DateTimeOffset CreatedDateTime { get; init; }
    public DateTimeOffset UpdatedDateTime { get; set; }
    public bool IsOnline { get; init; }
    public bool IsCurrent { get; init; }

    public static ApiRevisionData From(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string apiId, CreateOrUpdateApiRequest request)
    {
        return new ApiRevisionData
        {
            ApiId =
                $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.ApiManagement/service/{apimName}/apis/{apiId}",
            ApiRevision = request.ApiRevision ?? "1",
            CreatedDateTime = DateTimeOffset.UtcNow,
            UpdatedDateTime = DateTimeOffset.UtcNow,
            IsCurrent = true,
            IsOnline = true
        };
    }

    public void Update(CreateOrUpdateApiRequest request)
    {
        UpdatedDateTime = DateTimeOffset.UtcNow;
        ApiRevision = request.ApiRevision!;
    }

    public string GetApiId()
    {
        return ApiId!.Split('/').Last();
    }
}