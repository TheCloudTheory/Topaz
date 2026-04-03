using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Models.Responses;

public record ListSubscriptionResourcesResponse
{
    public GenericResourceExpanded[]? Value { get; init; }
    public string? NextLink { get; init; }

    [UsedImplicitly]
    public record GenericResourceExpanded
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
        public string? Location { get; init; }
        public Dictionary<string, string>? Tags { get; init; }
        public object? Properties { get; init; }

        public static GenericResourceExpanded From<T>(ArmResource<T> resource)
        {
            return new GenericResourceExpanded
            {
                Id = resource.Id,
                Name = resource.Name,
                Type = resource.Type,
                Location = resource.Location,
                Tags = resource.Tags as Dictionary<string, string>,
                Properties = resource.Properties,
            };
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}