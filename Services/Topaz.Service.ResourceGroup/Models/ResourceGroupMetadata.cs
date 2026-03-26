using System.Text.Json;
using Azure.Core;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Models;

public record ResourceGroupMetadata
{
    public string Id { get; init; }
    public string Name { get; init; }
    public static string Type => "Microsoft.Resources/resourceGroups";
    public string Location { get; init; }

    public ResourceGroupMetadata(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location)
    {
        Id = $"/subscriptions/{subscriptionIdentifier.Value}/resourceGroups/{resourceGroupIdentifier.Value}";
        Name = resourceGroupIdentifier.Value;
        Location = location;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}