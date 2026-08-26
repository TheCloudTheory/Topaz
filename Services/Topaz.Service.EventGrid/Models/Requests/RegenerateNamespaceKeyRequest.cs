using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Requests;

internal sealed class RegenerateNamespaceKeyRequest : IValidatable
{
    public string? KeyName { get; init; }
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (string.IsNullOrWhiteSpace(KeyName))
        {
            return (false, "KeyName is required");
        }

        if (KeyName != "key1" && KeyName != "key2")
        {
            return (false, "KeyName should be 'key1', 'key2'");
        }
        
        return  (true, null);
    }
}