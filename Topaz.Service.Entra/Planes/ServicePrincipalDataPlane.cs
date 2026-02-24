using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Planes;

internal sealed class ServicePrincipalDataPlane(EntraResourceProvider provider, ITopazLogger logger)
{
    public static ServicePrincipalDataPlane New(ITopazLogger logger) => new(new EntraResourceProvider(logger), logger);
    
    public DataPlaneOperationResult<ServicePrincipal> Create(CreateServicePrincipalRequest request)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Create), "Creating a service principal `{0}`.", request.AppId);
        
        var entityPath = BuildLocalServicePrincipalEntityPath(ServicePrincipalIdentifier.From(request.AppId));

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<ServicePrincipal>.ForDuplicate("appId");
        }

        var servicePrincipal = ServicePrincipal.FromRequest(request);
        File.WriteAllText(entityPath, servicePrincipal.ToString());
        
        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Created, servicePrincipal, null, null);
    }
    
    private string BuildLocalServicePrincipalEntityPath(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        var path = provider.GetServiceInstanceServicePrincipalsDataPath();
        var fileName = $"{servicePrincipalIdentifier}.json";
        var entityPath = Path.Combine(path, fileName);
        return entityPath;
    }
    
    public DataPlaneOperationResult<ServicePrincipal> Get(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Get), "Fetching a service principal `{0}`.", servicePrincipalIdentifier);

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
                .Select(file => JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(file), GlobalSettings.JsonOptions))
                .SingleOrDefault(u => u?.Id == servicePrincipalIdentifier.Value);

            return servicePrincipal == null
                ? BadRequestOperationResult<ServicePrincipal>.ForNotFound(servicePrincipalIdentifier)
                : new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Success, servicePrincipal, null, null);
        }

        servicePrincipal = JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions);
        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Success, servicePrincipal, null, null);
    }

    public DataPlaneOperationResult<ServicePrincipal> Delete(ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Delete), "Deleting a service principal `{0}`.", servicePrincipalIdentifier);

        var existingUserOperation = Get(servicePrincipalIdentifier);
        if (existingUserOperation.Result == OperationResult.NotFound || existingUserOperation.Resource == null)
        {
            return BadRequestOperationResult<ServicePrincipal>.ForNotFound(servicePrincipalIdentifier);
        }
        
        var entityPath = BuildLocalServicePrincipalEntityPath(servicePrincipalIdentifier);
        File.Delete(entityPath);
        
        return new DataPlaneOperationResult<ServicePrincipal>(OperationResult.Deleted, null, null, null);
    }
}