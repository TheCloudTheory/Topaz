using Topaz.Service.Insights.Models;
using Topaz.Service.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsDataPlane
{
    public static ApplicationInsightsDataPlane New => new();

    public DataPlaneOperationResult<IngestionEnvelope> Ingest(string instrumentationKey, string type, string content)
    {
        return new DataPlaneOperationResult<IngestionEnvelope>(OperationResult.Success, new IngestionEnvelope(), null,
            null);
    }
}