using System.Text.Json.Serialization;
using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ProductContractResourceProperties
{
    public string? DisplayName { get; set; }
    public bool ApprovalNeeded { get; set; }
    public string? Description { get; set; }
    public bool SubscriptionRequired { get; set; }
    public int SubscriptionLimits { get; set; }
    public string? Terms { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductState State { get; set; }

    internal enum ProductState
    {
        NotPublished,
        Published,
    }

    public static ProductContractResourceProperties From(CreateOrUpdateProductRequest request)
    {
        throw new NotImplementedException();
    }
}