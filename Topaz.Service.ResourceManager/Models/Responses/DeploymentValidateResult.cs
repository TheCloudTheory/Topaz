using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed record DeploymentValidateResult
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string Type => "Microsoft.Resources/deployments";
    public DeploymentResourceProperties? Properties { get; set; }
    public GenericErrorResponse? Error { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}