using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal sealed class KeyVaultFullResource : KeyVaultResource
{
    [UsedImplicitly]
    public KeyVaultFullResource()
    {
    }

    public KeyVaultFullResource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string keyVaultName,
        string location,
        IDictionary<string, string>? tags,
        KeyVaultResourceProperties properties) : base(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, location, tags, properties)
    {
    }

    public DateTimeOffset? DeletionDate  { get; set; }
    public DateTimeOffset? ScheduledPurgeDate  { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}