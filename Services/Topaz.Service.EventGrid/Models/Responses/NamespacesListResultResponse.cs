using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Responses;

internal sealed class NamespacesListResultResponse : TopazApiModel
{
    public string? NextLink { get; set; } = "";
    public EventGridNamespaceResource[] Value { get; set; } = [];

    public static NamespacesListResultResponse From(EventGridNamespaceResource[] value)
    {
        return new NamespacesListResultResponse
        {
            Value = value
        };
    }
}