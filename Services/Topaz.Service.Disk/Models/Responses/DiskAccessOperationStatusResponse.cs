using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Disk.Models.Responses;

internal sealed class DiskAccessOperationStatusResponse
{
    public string Status { get; init; } = string.Empty;
    public string? AccessSAS { get; init; }
    public DiskAccessOperationStatusProperties? Properties { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed class DiskAccessOperationStatusProperties
{
    public DiskAccessOperationOutput Output { get; init; } = new();
}

internal sealed class DiskAccessOperationOutput
{
    public string AccessSAS { get; init; } = string.Empty;
}
