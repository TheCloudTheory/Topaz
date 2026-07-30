using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CheckNameAvailabilityRequest : IValidatable
{
    public string? Name { get; init; }
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return string.IsNullOrWhiteSpace(Name) ? (false, "Name is required") : (true, null);
    }
}