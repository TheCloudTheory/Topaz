using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models.Responses;

internal sealed class EventTypesListResultResponse : TopazApiModel
{
    public EventTypesListResultResponseProperties[]? Value { get; set; }

    internal class EventTypesListResultResponseProperties
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public InlineEventProperties? Properties { get; set; }
    }

    public static EventTypesListResultResponse From(EventGridTopicResource topic)
    {
        if (topic.Properties.EventTypeInfo == null || topic.Properties.EventTypeInfo.InlineEventTypes == null)
        {
            return new EventTypesListResultResponse();
        }

        return new EventTypesListResultResponse
        {
            Value =
            [
                .. topic.Properties.EventTypeInfo.InlineEventTypes.Select(typeInfo =>
                    new EventTypesListResultResponseProperties
                    {
                        Id =
                            $"providers/Microsoft.EventGrid/topicTypes/Microsoft.Storage.StorageAccounts/eventTypes/{typeInfo.Key}",
                        Name = typeInfo.Key,
                        Type = "Microsoft.EventGrid/topicTypes/eventTypes",
                        Properties = typeInfo.Value
                    })
            ]
        };
    }
}