using Topaz.Service.Shared;

namespace Topaz.Service.Insights.Models;

internal sealed class IngestionEnvelope : TopazApiModel
{
    public int ItemsReceived { get; set; } = 0;
    public int ItemsAccepted { get; set; } = 0;
    public object[]? Errors { get; set; }
}