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
    private readonly ResourceProvider _resourceProvider = new(logger);
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger), logger);
    private readonly BlobServiceDataPlane _dataPlane = new(new BlobServiceControlPlane(new BlobResourceProvider(logger), logger), logger);
    
    public (int Port, Protocol Protocol) PortAndProtocol => (8891, Protocol.Http);

    public string[] Endpoints => [
        "PUT /{storageAccountName}/{containerName}",
        "GET /{storageAccountName}/",
        "GET /{storageAccountName}/{containerName}",
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();
        
        if(TryGetStorageAccountName(path, out var storageAccountName) == false)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }

        try
        {
            var containerName = GetContainerName(path);
            
            logger.LogDebug($"Executing {nameof(GetResponse)}: Found container: {containerName}");

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
                    if (query.TryGetValueForKey("restype", out var restype) && restype == "container")
                    {
                        HandleListBlobsRequest(storageAccountName, containerName, response);
                    }
                    else
                    {
                        HandleGetContainersRequest(storageAccountName, response);
                    }
                    
                    return response;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }

        return response;
    }

    private void HandleListBlobsRequest(string storageAccountName, string containerName, HttpResponseMessage response)
    {
        // TODO: The request may come with additional keys in the query string, e.g.:
        // ?restype=container&comp=list&prefix=localhost/eh-test/$default/ownership/&include=Metadata  
        // We need to handle them as well
        
        var blobs = this._dataPlane.ListBlobs(storageAccountName, containerName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(BlobEnumerationResult));
        serializer.Serialize(sw, blobs);
        
        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetContainersRequest(string storageAccountName, HttpResponseMessage response)
    {
        var containers = this._controlPlane.ListContainers(storageAccountName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(ContainerEnumerationResult));
        serializer.Serialize(sw, containers);
        
        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateContainerRequest(string storageAccountName, string containerName, HttpResponseMessage response)
    {
        var code = this._controlPlane.CreateContainer(containerName, storageAccountName);
        
        response.StatusCode = code;
    }

    private string GetContainerName(string path)
    {
        logger.LogDebug($"Executing {nameof(GetContainerName)}: {path}");

        var pathParts = path.Split('/');
        var newPath = string.Join('/', pathParts.Skip(2));

        logger.LogDebug($"Executing {nameof(GetContainerName)}: New path: {newPath}");

        return newPath;
    }
    
    private bool TryGetStorageAccountName(string path, out string name)
    {
        logger.LogDebug($"Executing {nameof(TryGetStorageAccountName)}: {path}");

        var pathParts = path.Split('/');
        var accountName = pathParts[1];
        name = accountName;

        logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        return this._resourceProvider.CheckIfStorageAccountExists(accountName);
    }
}
