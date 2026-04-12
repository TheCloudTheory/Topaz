namespace Topaz.Service.Entra.Models.Requests;

internal sealed class CreateGroupRequest
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? MailNickname { get; init; }
    public bool? MailEnabled { get; init; }
    public bool? SecurityEnabled { get; init; }
    public string[]? GroupTypes { get; init; }
    public string[]? Owners { get; init; }
    public string[]? Members { get; init; }
    public bool? IsAssignableToRole { get; init; }
    public string? Visibility { get; init; }
    public string? MembershipRule { get; init; }
    public string? MembershipRuleProcessingState { get; init; }
    public string? Classification { get; init; }
    public string? Theme { get; init; }
    public string? PreferredLanguage { get; init; }
    public string? PreferredDataLocation { get; init; }
}
