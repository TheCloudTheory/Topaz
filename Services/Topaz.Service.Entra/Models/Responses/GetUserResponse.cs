using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class GetUserResponse
{
    public string? Id { get; set; }

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

    public static GetUserResponse From(User user)
    {
        return new GetUserResponse
        {
            Id = user.Id,
            BusinessPhones = user.BusinessPhones,
            DisplayName = user.DisplayName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            Mail = user.Mail,
            UserPrincipalName = user.UserPrincipalName,
            MailNickname = user.MailNickname,
            JobTitle = user.JobTitle,
            MobilePhone = user.MobilePhone,
            OfficeLocation = user.OfficeLocation,
            PreferredLanguage = user.PreferredLanguage,
            AccountEnabled = user.AccountEnabled,
            CreatedDateTime = user.CreatedDateTime,
            OtherMails = user.OtherMails ?? [],
            OnPremisesSamAccountName = user.OnPremisesSamAccountName,
            OnPremisesImmutableId = user.OnPremisesImmutableId,
            Department = user.Department,
            CompanyName = user.CompanyName,
            EmployeeId = user.EmployeeId,
        };
    }
}