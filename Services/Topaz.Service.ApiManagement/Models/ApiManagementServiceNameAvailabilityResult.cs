using System.Text.Json.Serialization;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiManagementServiceNameAvailabilityResult
{
    public string? Message { get; init; }
    public bool NameAvailable { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NameAvailabilityReason Reason { get; init; }
    
    public static ApiManagementServiceNameAvailabilityResult ForInvalidName()
    {
        return new ApiManagementServiceNameAvailabilityResult
        {
            NameAvailable = true,
            Reason = NameAvailabilityReason.Invalid,
            Message = "The name is invalid. Name should be between 1 and 50 characters long."
        };
    }

    public static ApiManagementServiceNameAvailabilityResult ForAlreadyExists()
    {
        return new ApiManagementServiceNameAvailabilityResult
        {
            NameAvailable = false,
            Reason = NameAvailabilityReason.AlreadyExists,
            Message = "The name is already taken."
        };
    }

    public static ApiManagementServiceNameAvailabilityResult ForValidName()
    {
        return new ApiManagementServiceNameAvailabilityResult
        {
            NameAvailable = true,
            Reason = NameAvailabilityReason.Valid
        };
    }

}

internal enum NameAvailabilityReason
{
    Valid,
    Invalid,
    AlreadyExists
}