namespace Topaz.Service.Entra.Models.Requests;

internal sealed class CreateUserRequest
{
	// Core (required by Graph when creating a user)
	public bool? AccountEnabled { get; init; }
	public required string DisplayName { get; init; }
	public required string MailNickname { get; init; }
	public required string UserPrincipalName { get; init; }
	public required PasswordProfileData PasswordProfile { get; init; }

	// Optional profile fields
	public string? GivenName { get; init; }
	public string? Surname { get; init; }
	public string? JobTitle { get; init; }
	public string? Mail { get; init; }
	public string? MobilePhone { get; init; }
	public string? OfficeLocation { get; init; }
	public string? PreferredLanguage { get; init; }
	public string[]? BusinessPhones { get; init; }
	public string[]? OtherMails { get; init; }

	// Additional directory attributes
	public string? OnPremisesSamAccountName { get; init; }
	public string? OnPremisesImmutableId { get; init; }
	public string? Department { get; init; }
	public string? CompanyName { get; init; }
	public string? EmployeeId { get; init; }

	internal sealed class PasswordProfileData
	{
		public required string Password { get; init; }
		public bool? ForceChangePasswordNextSignIn { get; init; }
	}
}