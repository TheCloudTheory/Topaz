namespace Topaz.Service.Entra.Models.Requests;

public class AddApplicationPasswordRequest
{
    public PasswordCredentialData? PasswordCredential { get; init; }

    public class PasswordCredentialData
    {
        public string? DisplayName { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public DateTimeOffset? StartDateTime { get; init; } = DateTimeOffset.UtcNow;

        public PasswordCredentialData()
        {
            EndDateTime = StartDateTime?.AddYears(2);
        }
    }
}