namespace Topaz.Service.Storage.Models.Requests;

internal sealed record CheckStorageAccountNameAvailabilityRequest
{
    public required string Name { get; init; }
    public string? Type { get; init; }
}