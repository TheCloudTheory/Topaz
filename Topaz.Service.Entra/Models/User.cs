using System.Text.Json;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal sealed class User : DirectoryObject
{
    // --- Core identity/profile ---
    public bool? AccountEnabled { get; set; }
    public string? DisplayName { get; init; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? UserType { get; set; }

    // --- Mail/phones ---
    public string? Mail { get; set; }
    public string? MailNickname { get; set; }
    public string[] BusinessPhones { get; init; } = [];
    public string? MobilePhone { get; set; }
    public string? FaxNumber { get; set; }
    public string[] ImAddresses { get; set; } = [];
    public string[] OtherMails { get; set; } = [];
    public string[] ProxyAddresses { get; set; } = [];

    // --- Organization / job ---
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? CompanyName { get; set; }
    public string? EmployeeId { get; set; }
    public string? EmployeeType { get; set; }
    public DateTimeOffset? EmployeeHireDate { get; set; }
    public JsonElement? EmployeeOrgData { get; set; } // complex

    // --- Location / address ---
    public string? OfficeLocation { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetAddress { get; set; }
    public string? UsageLocation { get; set; }

    // --- Preferences / misc profile ---
    public string? AboutMe { get; set; }
    public DateTimeOffset? Birthday { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredDataLocation { get; set; }
    public string? MySite { get; set; }
    public bool? ShowInAddressList { get; set; }

    // --- Lifecycle / timestamps ---
    public DateTimeOffset? CreatedDateTime { get; set; }
    public DateTimeOffset? LastPasswordChangeDateTime { get; set; }
    public DateTimeOffset? RefreshTokensValidFromDateTime { get; set; }
    public DateTimeOffset? SignInSessionsValidFromDateTime { get; set; }

    // --- External identities / B2B ---
    public string? ExternalUserState { get; set; }
    public DateTimeOffset? ExternalUserStateChangeDateTime { get; set; }
    public string? CreationType { get; set; }
    public JsonElement[] Identities { get; set; } = []; // complex (objectIdentity)

    // --- Password / auth policies ---
    public string? PasswordPolicies { get; set; }
    public PasswordProfileData? PasswordProfile { get; set; }
    
    internal sealed class PasswordProfileData
    {
        public string? Password { get; set; }
        public bool? ForceChangePasswordNextSignIn { get; set; }
        public bool? ForceChangePasswordNextSignInWithMfa { get; set; }
    }

    // --- Licensing / plans (complex) ---
    public JsonElement[] AssignedLicenses { get; set; } = [];
    public JsonElement[] AssignedPlans { get; set; } = [];
    public JsonElement[] LicenseAssignmentStates { get; set; } = [];
    public JsonElement[] ProvisionedPlans { get; set; } = [];

    // --- Age / consent (directory-dependent) ---
    public string? AgeGroup { get; set; }
    public string? ConsentProvidedForMinor { get; set; }
    public string? LegalAgeGroupClassification { get; set; }

    // --- On-premises (hybrid) ---
    public string? OnPremisesSamAccountName { get; set; }
    public string? OnPremisesImmutableId { get; set; }
    public string? OnPremisesUserPrincipalName { get; set; }
    public string? OnPremisesDistinguishedName { get; set; }
    public string? OnPremisesDomainName { get; set; }
    public string? OnPremisesSecurityIdentifier { get; set; }
    public bool? OnPremisesSyncEnabled { get; set; }
    public DateTimeOffset? OnPremisesLastSyncDateTime { get; set; }
    public JsonElement? OnPremisesExtensionAttributes { get; set; } // complex
    public JsonElement[] OnPremisesProvisioningErrors { get; set; } = []; // complex

    // --- Resource accounts ---
    public bool? IsResourceAccount { get; set; }
    
    public static User FromRequest(CreateUserRequest request, Guid? id = null)
    {
        return new User
        {
            Id = id.HasValue ? id.Value.ToString() : Guid.NewGuid().ToString(),

            // Core identity/profile
            AccountEnabled = request.AccountEnabled ?? true,
            DisplayName = request.DisplayName,
            GivenName = request.GivenName,
            Surname = request.Surname,
            UserPrincipalName = request.UserPrincipalName,

            // Mail/phones
            Mail = request.Mail,
            MailNickname = request.MailNickname,
            BusinessPhones = request.BusinessPhones ?? [],
            MobilePhone = request.MobilePhone,
            OtherMails = request.OtherMails ?? [],

            // Organization / job
            JobTitle = request.JobTitle,
            Department = request.Department,
            CompanyName = request.CompanyName,
            EmployeeId = request.EmployeeId,

            // Location
            OfficeLocation = request.OfficeLocation,

            // Preferences
            PreferredLanguage = request.PreferredLanguage,

            PasswordProfile = new PasswordProfileData
            {
                Password = request.PasswordProfile.Password,
                ForceChangePasswordNextSignIn = request.PasswordProfile.ForceChangePasswordNextSignIn ?? false,
                ForceChangePasswordNextSignInWithMfa = request.PasswordProfile.ForceChangePasswordNextSignIn ?? false,
            },

            // On-premises (hybrid)
            OnPremisesSamAccountName = request.OnPremisesSamAccountName,
            OnPremisesImmutableId = request.OnPremisesImmutableId,

            // Local/emulator bookkeeping
            CreatedDateTime = DateTimeOffset.UtcNow,
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}