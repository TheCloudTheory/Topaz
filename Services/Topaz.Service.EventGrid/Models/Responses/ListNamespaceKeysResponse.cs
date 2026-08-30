using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Responses;

internal sealed class ListNamespaceKeysResponse : TopazApiModel
{
    public string? Key1 { get; set; }
    public string? Key2 { get; set; }

    public static ListNamespaceKeysResponse From(EventGridSharedAccessKey[] keys)
    {
        return new ListNamespaceKeysResponse
        {
            Key1 = keys.Single(key => key.KeyName == "key1").KeyValue,
            Key2 = keys.Single(key => key.KeyName == "key2").KeyValue
        };
    }
}