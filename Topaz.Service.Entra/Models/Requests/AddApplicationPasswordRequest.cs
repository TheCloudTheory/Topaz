namespace Topaz.Service.Entra.Models.Requests;

public class AddApplicationPasswordRequest
{
    public string? DisplayName { get; set; }
    public DateTimeOffset? EndDateTime { get; set; }
    public DateTimeOffset? StartDateTime { get; set; } = DateTimeOffset.UtcNow;

    public AddApplicationPasswordRequest()
    {
        EndDateTime = StartDateTime?.AddYears(2);
    }
}