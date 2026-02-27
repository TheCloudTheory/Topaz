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

    public static GetUserResponse From(User operationResource)
    {
        return new GetUserResponse
        {
            Id = operationResource.Id,
            BusinessPhones = operationResource.BusinessPhones,
            DisplayName = operationResource.DisplayName,
            GivenName = operationResource.GivenName,
            Surname = operationResource.Surname,
            Mail = operationResource.Mail,
            UserPrincipalName = operationResource.UserPrincipalName,
            MailNickname = operationResource.MailNickname,
            JobTitle = operationResource.JobTitle,
            MobilePhone = operationResource.MobilePhone,
            OfficeLocation = operationResource.OfficeLocation,
            PreferredLanguage = operationResource.PreferredLanguage,
            AccountEnabled = operationResource.AccountEnabled,
            CreatedDateTime = operationResource.CreatedDateTime,
            OtherMails = operationResource.OtherMails ?? [],
            OnPremisesSamAccountName = operationResource.OnPremisesSamAccountName,
            OnPremisesImmutableId = operationResource.OnPremisesImmutableId,
            Department = operationResource.Department,
            CompanyName = operationResource.CompanyName,
            EmployeeId = operationResource.EmployeeId,
        };
    }
}