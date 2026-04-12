using System.Text.Json;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal class Group : DirectoryObject
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? MailNickname { get; set; }
    public bool? MailEnabled { get; set; }
    public bool? SecurityEnabled { get; set; }
    public string[]? GroupTypes { get; set; }
    public bool? IsAssignableToRole { get; set; }
    public string? Visibility { get; set; }
    public string? MembershipRule { get; set; }
    public string? MembershipRuleProcessingState { get; set; }
    public string? Classification { get; set; }
    public string? Theme { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredDataLocation { get; set; }
    public DateTimeOffset? CreatedDateTime { get; set; }
    public bool? OnPremisesSyncEnabled { get; set; }
    public string? OnPremisesLastSyncDateTime { get; set; }
    public string[]? ProxyAddresses { get; set; }
    public string[]? RenewedDateTime { get; set; }
    public string? ExpirationDateTime { get; set; }

    public static Group FromRequest(CreateGroupRequest request, Guid? id = null)
    {
        return new Group
        {
            Id = id.HasValue ? id.Value.ToString() : Guid.NewGuid().ToString(),
            DisplayName = request.DisplayName,
            Description = request.Description,
            MailNickname = request.MailNickname ?? request.DisplayName?.ToLowerInvariant().Replace(" ", "-"),
            MailEnabled = request.MailEnabled ?? false,
            SecurityEnabled = request.SecurityEnabled ?? false,
            GroupTypes = request.GroupTypes ?? [],
            IsAssignableToRole = request.IsAssignableToRole,
            Visibility = request.Visibility,
            MembershipRule = request.MembershipRule,
            MembershipRuleProcessingState = request.MembershipRuleProcessingState,
            Classification = request.Classification,
            Theme = request.Theme,
            PreferredLanguage = request.PreferredLanguage,
            PreferredDataLocation = request.PreferredDataLocation,
            CreatedDateTime = DateTimeOffset.UtcNow,
            OnPremisesSyncEnabled = false,
            ProxyAddresses = [],
        };
    }

    public void UpdateFromRequest(UpdateGroupRequest request)
    {
        if (request.DisplayName != null) DisplayName = request.DisplayName;
        if (request.Description != null) Description = request.Description;
        if (request.MailNickname != null) MailNickname = request.MailNickname;
        if (request.MailEnabled.HasValue) MailEnabled = request.MailEnabled;
        if (request.SecurityEnabled.HasValue) SecurityEnabled = request.SecurityEnabled;
        if (request.GroupTypes != null) GroupTypes = request.GroupTypes;
        if (request.IsAssignableToRole.HasValue) IsAssignableToRole = request.IsAssignableToRole;
        if (request.Visibility != null) Visibility = request.Visibility;
        if (request.MembershipRule != null) MembershipRule = request.MembershipRule;
        if (request.MembershipRuleProcessingState != null) MembershipRuleProcessingState = request.MembershipRuleProcessingState;
        if (request.Classification != null) Classification = request.Classification;
        if (request.Theme != null) Theme = request.Theme;
        if (request.PreferredLanguage != null) PreferredLanguage = request.PreferredLanguage;
        if (request.PreferredDataLocation != null) PreferredDataLocation = request.PreferredDataLocation;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
