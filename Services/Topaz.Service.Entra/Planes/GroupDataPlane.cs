using System.Text.Json;
using Topaz.Service.Entra.Models;
using Topaz.Service.Shared;
using Topaz.Shared;
using Directory = System.IO.Directory;

namespace Topaz.Service.Entra.Planes;

internal sealed class GroupDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public static GroupDataPlane New(ITopazLogger logger) => new(new EntraResourceProvider(logger), logger);
    
    public DataPlaneOperationResult<Group[]> ListServicePrincipals()
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(ListServicePrincipals), "Listing groups");

        var path = provider.GetServiceInstanceServicePrincipalsDataPath();
        var files = Directory.EnumerateFiles(path, "*.json");

        return new DataPlaneOperationResult<Group[]>(OperationResult.Success,
            files.Select(file =>
                    JsonSerializer.Deserialize<Group>(File.ReadAllText(file), GlobalSettings.JsonOptions)!)
                .ToArray(),
            null, null);
    }
}