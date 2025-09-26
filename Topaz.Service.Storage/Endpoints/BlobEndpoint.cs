using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints;

public class BlobEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceProvider _resourceProvider = new(logger);
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultBlobStoragePort, Protocol.Http);

    public string[] Endpoints =>
    [
        "PUT /{containerName}",
        "PUT /{containerName}/...",
        "GET /{containerName}",
        "GET /",
        "HEAD /{containerName}/...",
        "DELETE /{containerName}/...",
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        if (!TryGetStorageAccountName(headers, out var storageAccountName))
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
                if (query.TryGetValueForKey("restype", out var restype) && restype == "container")
                {
                    HandleCreateContainerRequest(storageAccountName, containerName, response);
                    return response;
                }

                if (TryGetBlobName(path, out var blobName))
                {
                    if (query.TryGetValueForKey("comp", out var comp) && comp == "metadata")
                    {
                        HandleSetBlobMetadataRequest(storageAccountName, containerName, headers, response);
                    }
                    else
                    {
                        HandleUploadBlobRequest(storageAccountName, path, blobName!, input, response);
                    }
                }
            }

            if (method == "GET")
            {
                if (query.TryGetValueForKey("comp", out var comp) && comp == "list")
                {
                    if (query.TryGetValueForKey("restype", out var restype) && restype == "container")
                    {
                        HandleListBlobsRequest(storageAccountName, containerName, response);
                    }
                    else
                    {
                        HandleGetContainersRequest(storageAccountName, response);
                    }
                }
            }

            if (method == "HEAD")
            {
                if (TryGetBlobName(path, out var blobName))
                {
                    HandleGetBlobPropertiesRequest(storageAccountName, path, blobName!, response);
                }
            }

            if (method == "DELETE")
            {
                if (TryGetBlobName(containerName, out var blobName))
                {
                    HandleDeleteBlobRequest(storageAccountName, containerName, blobName!, response);
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

    private void HandleSetBlobMetadataRequest(string storageAccountName, string containerName,
        IHeaderDictionary headers, HttpResponseMessage response)
    {
        var result = _dataPlane.SetBlobMetadata(storageAccountName, containerName, headers);

        if (result == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode = result;
        }
    }

    private void HandleDeleteBlobRequest(string storageAccountName, string containerName, string blobName,
        HttpResponseMessage response)
    {
        var result = _dataPlane.DeleteBlob(storageAccountName, containerName, blobName);

        if (result == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode = result;
        }
    }

    private void HandleGetBlobPropertiesRequest(string storageAccountName, string blobPath, string blobName,
        HttpResponseMessage response)
    {
        var properties = _dataPlane.GetBlobProperties(storageAccountName, blobPath, blobName);

        if (properties.code == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode = properties.code;

            if (properties.properties != null)
            {
                response.Headers.Add("x-ms-meta-Name", properties.properties.Name);
            }
        }
    }

    private void HandleUploadBlobRequest(string storageAccountName, string blobPath, string blobName, Stream input,
        HttpResponseMessage response)
    {
        var result = _dataPlane.PutBlob(storageAccountName, blobPath, blobName, input);

        // TODO: The response must include the response headers from https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#response
        response.StatusCode = result.code;

        if (result.properties == null) return;

        // Note we're enclosing ETag header explicitly with double quotes to align it 
        // with RFC description stating this tag is "nn entity tag consists of an opaque quoted string, possibly prefixed by a weakness indicator"
        response.Headers.Add("ETag", $"\"{result.properties.ETag.ToString()}\"");

        // Adding `Last-Modified` directly as response header fail with an error stating
        // we're misusing that header. However, based on the behavior of Blob Storage SDK 
        // it looks like it expects that header to be part of the response headers, not response
        // content. For now we can leave it as it is (as SDK fallbacks to ETag anyway),
        // but it may be worth considering adding that header without validation if possible.
        var emptyContent = new StringContent(string.Empty);
        emptyContent.Headers.LastModified = result.properties.LastModified;
        response.Content = emptyContent;
    }

    private bool TryGetBlobName(string blobPath, out string? blobName)
    {
        var matches = Regex.Match(blobPath, @"[^/]+$", RegexOptions.Compiled);
        if (matches.Success)
        {
            blobName = matches.Groups[0].Value;
            return true;
        }

        blobName = null;
        return false;
    }

    private void HandleListBlobsRequest(string storageAccountName, string containerName, HttpResponseMessage response)
    {
        // TODO: The request may come with additional keys in the query string, e.g.:
        // ?restype=container&comp=list&prefix=localhost/eh-test/$default/ownership/&include=Metadata  
        // We need to handle them as well

        var blobs = _dataPlane.ListBlobs(storageAccountName, containerName);

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

    private void HandleCreateContainerRequest(string storageAccountName, string containerName,
        HttpResponseMessage response)
    {
        var code = _controlPlane.CreateContainer(containerName, storageAccountName);

        response.StatusCode = code;
    }

    private string GetContainerName(string path)
    {
        logger.LogDebug($"Executing {nameof(GetContainerName)}: {path}");

        var pathParts = path.Split('/');

        logger.LogDebug($"Executing {nameof(GetContainerName)}: Returning: {pathParts[1]}");

        return pathParts[1];
    }

    private bool TryGetStorageAccountName(IHeaderDictionary headers, out string? name)
    {
        logger.LogDebug($"Executing {nameof(TryGetStorageAccountName)}");

        if (!headers.TryGetValue("Host", out var host))
        {
            logger.LogError("`Host` header not found - it's required for storage account creation.");
            
            name = null;
            return false;
        }
        
        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];
        name = accountName;

        logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        return _resourceProvider.CheckIfStorageAccountExists(accountName);
    }
}