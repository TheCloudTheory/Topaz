using System.Text.Json.Serialization;

namespace Topaz.Chaos.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FaultType
{
    Timeout,
    TransientError,
    Throttle,
    ServiceUnavailable
}
