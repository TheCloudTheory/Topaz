using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Models.Responses;

internal sealed class GetManagementGroupResponse
{
    public string Id { get; init; } = string.Empty;
    public string Type => "Microsoft.Management/managementGroups";
    public string Name { get; init; } = string.Empty;
    public ManagementGroupResponseProperties Properties { get; set; } = new();
    
    internal sealed class ManagementGroupResponseProperties
    {
        public string TenantId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ManagementGroupDetails Details { get; set; } = new();
        public ManagementGroupChildResponse[]? Children { get; set; }

        internal class ManagementGroupChildResponse
        {
            public string Id { get; init; } = string.Empty;
            public string Type => "Microsoft.Management/managementGroups";
            public string Name { get; init; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;

            public static ManagementGroupChildResponse From(ManagementGroupProperties.ManagementGroupChild managementGroup)
            {
                return new ManagementGroupChildResponse
                {
                    Id = managementGroup.Id,
                    Name = managementGroup.Name,
                    DisplayName = managementGroup.DisplayName
                };
            }
        }
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static GetManagementGroupResponse From(ManagementGroup managementGroup)
    {
        return new GetManagementGroupResponse
        {
            Id = managementGroup.Id,
            Name = managementGroup.Name,
            Properties = new ManagementGroupResponseProperties
            {
                TenantId = managementGroup.Properties.TenantId,
                DisplayName = managementGroup.Properties.DisplayName,
                Details = managementGroup.Properties.Details,
                Children = managementGroup.Properties.Children
                    ?.Select(ManagementGroupResponseProperties.ManagementGroupChildResponse.From).ToArray()
            }
        };
    }
}