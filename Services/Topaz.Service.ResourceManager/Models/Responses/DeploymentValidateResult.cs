using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed record DeploymentValidateResult
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string Type => "Microsoft.Resources/deployments";
    public DeploymentResourceProperties? Properties { get; set; }

    internal static DeploymentValidateResult FromRequest(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName,
        CreateDeploymentRequest request)
    {
        return new DeploymentValidateResult
        {
            Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.Resources/deployments/{deploymentName}",
            Name = deploymentName,
            Properties = DeploymentResourceProperties.ForValidate(request)
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}