using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class DeletedServiceContractResponse : TopazApiModel
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? Type => "Microsoft.ApiManagement/deletedservices";
    public DeletedServiceContractProperties?  Properties { get; init; }

    internal class DeletedServiceContractProperties
    {
        public string? DeletionDate { get; init; }
        public string? ScheduledPurgeDate { get; init; }
        public string? ServiceId { get; init; }
    }

    public static DeletedServiceContractResponse From(ApiManagementServiceFullResource existingResource)
    {
        return new DeletedServiceContractResponse
        {
            Id =
                $"/subscriptions/{existingResource.GetSubscription()}/providers/Microsoft.ApiManagement/locations/{existingResource.Location}/deletedservices/{existingResource.Name}",
            Name = existingResource.Name,
            Location = existingResource.Location,
            Properties = new DeletedServiceContractProperties
            {
                DeletionDate = existingResource.DeletionDate?.ToString(),
                ScheduledPurgeDate = existingResource.ScheduledPurgeDate?.ToString(),
                ServiceId = existingResource.Id
            }
        };
    }
}