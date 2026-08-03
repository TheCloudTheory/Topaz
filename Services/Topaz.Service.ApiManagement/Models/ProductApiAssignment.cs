namespace Topaz.Service.ApiManagement.Models;

internal sealed class ProductApiAssignment
{
    public string? ApiId { get; set; }
    public string? ProductId { get; set; }
    public string? ApimName { get; init; }

    public static ProductApiAssignment New(string existingApiId, string existingProductId, string apimName)
    {
        return new ProductApiAssignment
        {
            ApiId = existingApiId,
            ProductId = existingProductId,
            ApimName = apimName
        };
    }

    public string GetId()
    {
        return GetId(ProductId!, ApiId!);
    }

    public static string GetId(string productId, string apiId)
    {
        return $"{productId}-{apiId}";
    }

    public void UpdateFrom(string productId, string apiId)
    {
        ApiId = apiId;
        ProductId = productId;
    }
}