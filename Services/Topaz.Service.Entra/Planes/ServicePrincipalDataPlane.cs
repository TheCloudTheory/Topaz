using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Directory = System.IO.Directory;

namespace Topaz.Service.Entra.Planes;

internal sealed class ServicePrincipalDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public static ServicePrincipalDataPlane New(ITopazLogger logger) => new(new EntraResourceProvider(logger), logger);

    public DataPlaneOperationResult<ServicePrincipal> Create(CreateServicePrincipalRequest request)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Create), "Creating a service principal `{0}`.",
            request.AppId);

        var entityPath = BuildLocalServicePrincipalEntityPath(ServicePrincipalIdentifier.From(request.AppId));

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<ServicePrincipal>.ForDuplicate("appId");
        }

        // If DisplayName is not provided, inherit it from the corresponding Application
        var effectiveDisplayName = request.DisplayName;
        if (string.IsNullOrEmpty(effectiveDisplayName))
        {
            var appPlane = new ApplicationsDataPlane(provider, logger);
            var appResult = appPlane.Get(ApplicationIdentifier.From(request.AppId));
            if (appResult.Result == OperationResult.Success && appResult.Resource != null)
            {
                effectiveDisplayName = appResult.Resource.DisplayName;
            }
        }

        var servicePrincipal = ServicePrincipal.FromRequest(request, effectiveDisplayName);
        File.WriteAllText(entityPath, servicePrincipal.ToString());

        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Created, servicePrincipal, null, null);
    }

    private string BuildLocalServicePrincipalEntityPath(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        var path = provider.GetServiceInstanceServicePrincipalsDataPath();
        var safeName = PathGuard.SanitizeName(servicePrincipalIdentifier.Value);
        var fileName = $"{safeName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);
        return entityPath;
    }

    public DataPlaneOperationResult<ServicePrincipal> Get(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Get), "Fetching a service principal `{0}`.",
            servicePrincipalIdentifier);

        ServicePrincipal? servicePrincipal;
        var entityPath = BuildLocalServicePrincipalEntityPath(servicePrincipalIdentifier);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Get),
                "Didn't find a service principal `{0}` so will perform a full search.", servicePrincipalIdentifier);

            // Service principal entities are created using `AppId` we if the first 
            // search fails, we may perform a secondary, more expensive check by looking
            // at all entities and looking for the given ID
            var path = provider.GetServiceInstanceServicePrincipalsDataPath();
            servicePrincipal = Directory.EnumerateFiles(path, "*.json")
                .Select(file =>
                    JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(file), GlobalSettings.JsonOptions))
                .SingleOrDefault(u => u?.Id == servicePrincipalIdentifier.Value);

            return servicePrincipal == null
                ? BadRequestOperationResult<ServicePrincipal>.ForNotFound(servicePrincipalIdentifier)
                : new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Success, servicePrincipal, null, null);
        }

        servicePrincipal =
            JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions);
        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Success, servicePrincipal, null, null);
    }

    public DataPlaneOperationResult<ServicePrincipal> Delete(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Delete), "Deleting a service principal `{0}`.",
            servicePrincipalIdentifier);

        var existingServicePrincipal = Get(servicePrincipalIdentifier);
        if (existingServicePrincipal.Result == OperationResult.NotFound || existingServicePrincipal.Resource == null)
        {
            return BadRequestOperationResult<ServicePrincipal>.ForNotFound(servicePrincipalIdentifier);
        }

        // Files are stored by AppId, not object Id — use the AppId from the retrieved resource
        var storedIdentifier = ServicePrincipalIdentifier.From(existingServicePrincipal.Resource.AppId);
        var entityPath = BuildLocalServicePrincipalEntityPath(storedIdentifier);
        File.Delete(entityPath);

        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Deleted, null, null, null);
    }

    public DataPlaneOperationResult Update(ServicePrincipalIdentifier servicePrincipalIdentifier,
        UpdateServicePrincipalRequest request)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Update), "Updating a service principal `{0}`.",
            servicePrincipalIdentifier);

        var existingServicePrincipal = Get(servicePrincipalIdentifier);
        if (existingServicePrincipal.Result == OperationResult.NotFound || existingServicePrincipal.Resource == null)
        {
            return BadRequestOperationResult.ForNotFound(servicePrincipalIdentifier);
        }

        existingServicePrincipal.Resource.UpdateFrom(request);

        // Files are stored by AppId, not object Id — use the AppId from the retrieved resource
        var storedIdentifier = ServicePrincipalIdentifier.From(existingServicePrincipal.Resource.AppId);
        var entityPath = BuildLocalServicePrincipalEntityPath(storedIdentifier);
        File.WriteAllText(entityPath, existingServicePrincipal.Resource.ToString());

        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public DataPlaneOperationResult<ServicePrincipal[]> ListServicePrincipals()
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(ListServicePrincipals), "Listing service principals");

        var path = provider.GetServiceInstanceServicePrincipalsDataPath();
        var files = Directory.EnumerateFiles(path, "*.json");

        return new DataPlaneOperationResult<ServicePrincipal[]>(OperationResult.Success,
            files.Select(file =>
                    JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(file), GlobalSettings.JsonOptions)!)
                .ToArray(),
            null, null);
    }
}