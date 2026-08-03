using System.Text.Json.Serialization;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateProductRequest
{
    public string? DisplayName { get; set; }
    public bool? ApprovalNeeded { get; set; }
    public string? Description { get; set; }
    public bool? SubscriptionRequired { get; set; }
    public int? SubscriptionLimits { get; set; }
    public string? Terms { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductContractResourceProperties.ProductState? State { get; set; }
}