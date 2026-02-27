using System.Text.Json;
using Topaz.Service.Entra.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Planes;

internal sealed class ApplicationsDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public DataPlaneOperationResult<Application[]> ListApplications()
    {
        logger.LogDebug(nameof(ApplicationsDataPlane), nameof(ListApplications), "Listing applications");

        var path = provider.GetServiceInstanceApplicationsDataPath();
        var files = Directory.EnumerateFiles(path, "*.json");

        return new DataPlaneOperationResult<Application[]>(OperationResult.Success,
            files.Select(file =>
                JsonSerializer.Deserialize<Application>(File.ReadAllText(file), GlobalSettings.JsonOptions)!).ToArray(),
            null, null);
    }
}