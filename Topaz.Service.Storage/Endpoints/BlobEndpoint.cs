using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints;

public class BlobEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    private readonly ResourceProvider resourceProvider = new(logger);

    private readonly BlobServiceControlPlane controlPlane =
        new BlobServiceControlPlane(new BlobResourceProvider(logger), logger);
    public (int Port, Protocol Protocol) PortAndProtocol => (8891, Protocol.Http);

    public string[] Endpoints => [
        "PUT /{storageAccountName}/{containerName}"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();
        
        if(TryGetStorageAccountName(path, out var storageAccountName) == false)
        {
            response.StatusCode = System.Net.HttpStatusCode.NotFound;
            return response;
        }

        try
        {
            var containerName = GetContainerName(path);
            
            this.logger.LogDebug($"Executing {nameof(GetResponse)}: Found container: {containerName}");

            if (method == "PUT")
            {
                if(query.TryGetValueForKey("restype", out var restype) && restype == "container")
                {
                    HandleCreateContainerRequest(storageAccountName, containerName, response);
                    return response;
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }

        return response;
    }

    private void HandleCreateContainerRequest(string storageAccountName, string containerName, HttpResponseMessage response)
    {
        var code = this.controlPlane.CreateContainer(containerName, storageAccountName);
        
        response.StatusCode = code;
    }

    private string GetContainerName(string path)
    {
        this.logger.LogDebug($"Executing {nameof(GetContainerName)}: {path}");

        var pathParts = path.Split('/');
        var newPath = string.Join('/', pathParts.Skip(3));

        this.logger.LogDebug($"Executing {nameof(GetContainerName)}: New path: {newPath}");

        return newPath;
    }
    
    private bool TryGetStorageAccountName(string path, out string name)
    {
        this.logger.LogDebug($"Executing {nameof(TryGetStorageAccountName)}: {path}");

        var pathParts = path.Split('/');
        var accountName = pathParts[2];
        name = accountName;

        this.logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        return this.resourceProvider.CheckIfStorageAccountExists(accountName);
    }
}
