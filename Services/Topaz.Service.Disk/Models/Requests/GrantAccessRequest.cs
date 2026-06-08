namespace Topaz.Service.Disk.Models.Requests;

public sealed class GrantAccessRequest
{
    public string? Access { get; set; }

    public int DurationInSeconds { get; set; }
}
