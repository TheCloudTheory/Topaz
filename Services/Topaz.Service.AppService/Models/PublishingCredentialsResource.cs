using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

internal sealed class PublishingCredentialsResource : ArmSubresource<PublishingCredentialsResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public PublishingCredentialsResource()
#pragma warning restore CS8618
    {
    }

    public PublishingCredentialsResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string appServiceName,
        string credentialName,
        PublishingCredentialsResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.Web/sites/{appServiceName}/config/publishingcredentials/{credentialName}";
        Name = credentialName;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Web/sites/config/publishingcredentials";
    public override PublishingCredentialsResourceProperties Properties { get; init; }
    
    public static PublishingCredentialsResource Create(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string appServiceName)
    {
        var userName = $"${appServiceName}";
        var scmUri = $"https://{appServiceName}.scm.azurewebsites.topaz.local.dev";
        var properties = PublishingCredentialsResourceProperties.Create(userName, scmUri);
        return new PublishingCredentialsResource(subscriptionIdentifier, resourceGroupIdentifier, appServiceName, userName, properties);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}