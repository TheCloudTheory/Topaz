using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

public sealed class GetUserResponse
{
    public string Id => Guid.NewGuid().ToString();

    public string[] BusinessPhones { get; set; } = [];

    public string? DisplayName { get; set; }

    public string? GivenName { get; set; }

    public string? Surname { get; set; }

    public string? Mail { get; set; }

    public string? UserPrincipalName { get; set; }

    public string? MailNickname { get; set; }

    public string? JobTitle { get; set; }

    public string? MobilePhone { get; set; }

    public string? OfficeLocation { get; set; }

    public string? PreferredLanguage { get; set; }

    public bool? AccountEnabled { get; set; }

    public DateTimeOffset? CreatedDateTime { get; set; }

    public string[] OtherMails { get; set; } = [];

    public string? OnPremisesSamAccountName { get; set; }

    public string? OnPremisesImmutableId { get; set; }

    public string? Department { get; set; }

    public string? CompanyName { get; set; }

    public string? EmployeeId { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}