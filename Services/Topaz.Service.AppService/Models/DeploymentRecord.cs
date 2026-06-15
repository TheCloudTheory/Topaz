using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

public sealed class DeploymentRecord
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Message { get; set; }
    public string Deployer { get; set; } = "Push Deployer";

    public static DeploymentRecord Succeeded(string id) => new()
    {
        Id = id,
        Status = "succeeded",
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow,
        Deployer = "Push Deployer"
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
