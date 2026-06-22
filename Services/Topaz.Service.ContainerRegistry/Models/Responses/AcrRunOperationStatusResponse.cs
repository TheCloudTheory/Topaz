using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

/// <summary>
/// ARM async-operation status object returned by the operationStatuses polling endpoint.
/// The .NET ContainerRegistry SDK polls this URL after receiving a 202 Accepted with
/// Azure-AsyncOperation header from scheduleRun / tasks/{name}/run.
/// </summary>
internal sealed class AcrRunOperationStatusResponse
{
    /// <summary>ARM async-operation status: InProgress | Succeeded | Failed | Canceled</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    /// <summary>Maps a run status to the ARM async-operation terminal vocabulary.</summary>
    public static string FromRunStatus(string runStatus) => runStatus switch
    {
        "Succeeded" => "Succeeded",
        "Failed"    => "Failed",
        "Error"     => "Failed",
        "Canceled"  => "Canceled",
        _           => "InProgress"   // Queued | Running
    };
}
