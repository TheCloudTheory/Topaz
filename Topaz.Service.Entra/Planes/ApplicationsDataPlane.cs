using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Models.Requests;
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

    public DataPlaneOperationResult<Application> Create(CreateApplicationRequest request)
    {
        logger.LogDebug(nameof(UserDataPlane), nameof(Create), "Creating an application `{0}`.", request.AppId);

        var applicationIdentifier = ApplicationIdentifier.From(Guid.NewGuid().ToString());
        var entityPath = BuildLocalApplicationEntityPath(applicationIdentifier);

        if (File.Exists(entityPath))
        {
            return BadRequestOperationResult<Application>.ForDuplicate("appId");
        }

        var application = Application.FromRequest(applicationIdentifier, request);
        File.WriteAllText(entityPath, application.ToString());

        return new DataPlaneOperationResult<Application>(OperationResult.Created, application, null, null);
    }

    private string BuildLocalApplicationEntityPath(ApplicationIdentifier applicationIdentifier)
    {
        var path = provider.GetServiceInstanceApplicationsDataPath();
        var fileName = $"{applicationIdentifier}.json";
        var entityPath = Path.Combine(path, fileName);
        return entityPath;
    }

    public DataPlaneOperationResult Update(ApplicationIdentifier applicationIdentifier,
        UpdateApplicationRequest request)
    {
        logger.LogDebug(nameof(ApplicationsDataPlane), nameof(Update), "Updating an application `{0}`.",
            applicationIdentifier);

        var existingApplication = Get(applicationIdentifier);
        if (existingApplication.Result == OperationResult.NotFound || existingApplication.Resource == null)
        {
            return BadRequestOperationResult.ForNotFound(applicationIdentifier);
        }

        existingApplication.Resource.UpdateFrom(request);

        var entityPath = BuildLocalApplicationEntityPath(applicationIdentifier);
        File.WriteAllText(entityPath, existingApplication.Resource.ToString());

        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public DataPlaneOperationResult<Application> Get(ApplicationIdentifier applicationIdentifier)
    {
        logger.LogDebug(nameof(ApplicationsDataPlane), nameof(Get), "Fetching an application `{0}`.",
            applicationIdentifier);

        Application? application;
        var entityPath = BuildLocalApplicationEntityPath(applicationIdentifier);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(ServicePrincipalDataPlane), nameof(Get),
                "Didn't find an application `{0}` so will perform a full search.", applicationIdentifier);
            
            // Application entities are created using the generated appId, but they may
            // be fetched using their object ID
            var path = provider.GetServiceInstanceApplicationsDataPath();
            application = Directory.EnumerateFiles(path, "*.json")
                .Select(file => JsonSerializer.Deserialize<Application>(File.ReadAllText(file), GlobalSettings.JsonOptions))
                .SingleOrDefault(u => u?.Id == applicationIdentifier.Value);

            return application == null
                ? BadRequestOperationResult<Application>.ForNotFound(applicationIdentifier)
                : new DataPlaneOperationResult<Application>(OperationResult.Success, application, null, null);
        }

        application = JsonSerializer.Deserialize<Application>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions);
        return new DataPlaneOperationResult<Application>(OperationResult.Success, application, null, null);
    }

    public DataPlaneOperationResult Delete(ApplicationIdentifier applicationIdentifier)
    {
        logger.LogDebug(nameof(ApplicationsDataPlane), nameof(Delete), "Deleting an application `{0}`.",
            applicationIdentifier);

        var existingServicePrincipal = Get(applicationIdentifier);
        if (existingServicePrincipal.Result == OperationResult.NotFound || existingServicePrincipal.Resource == null)
        {
            return BadRequestOperationResult.ForNotFound(applicationIdentifier);
        }

        var entityPath = BuildLocalApplicationEntityPath(applicationIdentifier);
        File.Delete(entityPath);

        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }
}