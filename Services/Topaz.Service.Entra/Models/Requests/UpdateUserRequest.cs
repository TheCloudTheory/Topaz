namespace Topaz.Service.Entra.Models.Requests;

internal sealed class UpdateUserRequest
{
    public bool? AccountEnabled { get; init; }
    public string? DisplayName { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? CompanyName { get; init; }
    public string? EmployeeId { get; init; }
    public string? EmployeeType { get; init; }
    public string? Mail { get; init; }
    public string? MailNickname { get; init; }
    public string? MobilePhone { get; init; }
    public string? OfficeLocation { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? StreetAddress { get; init; }
    public string? UsageLocation { get; init; }
    public string? PreferredLanguage { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? UserType { get; init; }
    public string? OnPremisesSamAccountName { get; init; }
    public string? OnPremisesImmutableId { get; init; }
    public string? PasswordPolicies { get; init; }
    public string[]? BusinessPhones { get; init; }
    public string[]? OtherMails { get; init; }
}
