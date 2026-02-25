using System.Text.Json;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal sealed class User : DirectoryObject
{
    public string[] BusinessPhones { get; init; } = [];
    public string? DisplayName { get; init; }
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
    
    public static User FromRequest(CreateUserRequest request, Guid? id = null)
    {
        return new User
        {
            Id = id.HasValue ? id.Value.ToString() : Guid.NewGuid().ToString(),
            BusinessPhones = request.BusinessPhones ?? [],
            DisplayName = request.DisplayName,
            GivenName = request.GivenName,
            Surname = request.Surname,
            Mail = request.Mail,
            UserPrincipalName = request.UserPrincipalName,
            MailNickname = request.MailNickname,
            JobTitle = request.JobTitle,
            MobilePhone = request.MobilePhone,
            OfficeLocation = request.OfficeLocation,
            PreferredLanguage = request.PreferredLanguage,
            AccountEnabled = request.AccountEnabled,
            CreatedDateTime = DateTimeOffset.UtcNow,
            OtherMails = request.OtherMails ?? [],
            OnPremisesSamAccountName = request.OnPremisesSamAccountName,
            OnPremisesImmutableId = request.OnPremisesImmutableId,
            Department = request.Department,
            CompanyName = request.CompanyName,
            EmployeeId = request.EmployeeId,
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}