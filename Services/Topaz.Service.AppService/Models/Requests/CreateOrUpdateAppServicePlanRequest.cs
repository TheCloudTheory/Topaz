namespace Topaz.Service.AppService.Models.Requests;

internal sealed record CreateOrUpdateAppServicePlanRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public AppServicePlanSkuDescription? Sku { get; init; }
    public AppServicePlanProperties? Properties { get; init; }

    internal sealed class AppServicePlanSkuDescription
    {
        public string? Name { get; init; }
        public string? Tier { get; init; }
        public string? Size { get; init; }
        public string? Family { get; init; }
        public int? Capacity { get; init; }
    }

    internal sealed class AppServicePlanProperties
    {
        public int? NumberOfWorkers { get; init; }
        public int? MaximumNumberOfWorkers { get; init; }
        public string? WorkerTierName { get; init; }
        public bool? HyperV { get; init; }
        public bool? IsSpot { get; init; }
        public bool? Reserved { get; init; }
    }
}
