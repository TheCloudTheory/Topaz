using Topaz.Service.Shared;

namespace Topaz.Service.Insights.Models;

internal sealed class IngestionEnvelope : TopazApiModel
{
    public int ItemsReceived { get; set; }
    public int ItemsAccepted { get; set; }
    public object[]? Errors { get; set; }
}