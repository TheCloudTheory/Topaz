using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
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
        "PUT /{storageAccountName}/{containerName}",
        "GET /{storageAccountName}/",
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

            if (method == "GET")
            {
                if(query.TryGetValueForKey("comp", out var comp) && comp == "list")
                {
                    HandleGetContainersRequest(storageAccountName, response);
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

    private void HandleGetContainersRequest(string storageAccountName, HttpResponseMessage response)
    {
        var containers = this.controlPlane.ListContainers(storageAccountName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(ContainerEnumerationResult));
        serializer.Serialize(sw, containers);
        
        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
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
        var newPath = string.Join('/', pathParts.Skip(2));

        this.logger.LogDebug($"Executing {nameof(GetContainerName)}: New path: {newPath}");

        return newPath;
    }
    
    private bool TryGetStorageAccountName(string path, out string name)
    {
        this.logger.LogDebug($"Executing {nameof(TryGetStorageAccountName)}: {path}");

        var pathParts = path.Split('/');
        var accountName = pathParts[1];
        name = accountName;

        this.logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        return this.resourceProvider.CheckIfStorageAccountExists(accountName);
    }
}
