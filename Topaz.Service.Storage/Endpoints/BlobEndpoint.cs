using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Services;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;
using BlobProperties = Topaz.Service.Storage.Models.BlobProperties;

namespace Topaz.Service.Storage.Endpoints;

public class BlobEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceProvider _resourceProvider = new(logger);
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultBlobStoragePort], Protocol.Http);

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
        var response = new HttpResponseMessage();

        if (!TryGetStorageAccount(headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        try
        {
            var containerName = GetContainerName(path);

            logger.LogDebug(nameof(BlobEndpoint), nameof(GetResponse), "Found container: {0}", containerName);

            switch (method)
            {
                case "PUT" when query.TryGetValueForKey("restype", out var restype) && restype == "container":
                    HandleCreateContainerRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, containerName, response);
                    return response;
                case "PUT":
                {
                    if (TryGetBlobName(path, out var blobName))
                    {
                        if (query.TryGetValueForKey("comp", out var comp) && comp == "metadata")
                        {
                            HandleSetBlobMetadataRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, path, blobName!, headers, response);
                        }
                        else
                        {
                            HandleUploadBlobRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, path, blobName!, input, response);
                        }
                    }

                    break;
                }
                case "GET":
                {
                    if (query.TryGetValueForKey("comp", out var comp) && comp == "list")
                    {
                        if (query.TryGetValueForKey("restype", out var restype) && restype == "container")
                        {
                            HandleListBlobsRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, containerName, response);
                        }
                        else
                        {
                            HandleGetContainersRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, response);
                        }
                    }

                    break;
                }
                case "HEAD":
                {
                    if (TryGetBlobName(path, out var blobName))
                    {
                        HandleGetBlobPropertiesRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, path, blobName!, response);
                    }

                    break;
                }
                case "DELETE":
                {
                    if (TryGetBlobName(path, out var blobName))
                    {
                        HandleDeleteBlobRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, path, blobName!, response);
                    }

                    break;
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

    private void HandleSetBlobMetadataRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName,
        IHeaderDictionary headers, HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleSetBlobMetadataRequest), "Handling setting blob metadata for {0}.", blobPath);
        
        var result = _dataPlane.SetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath, headers);

        if (result == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
            logger.LogDebug(nameof(BlobEndpoint), nameof(HandleSetBlobMetadataRequest), "Blob {0} was not found.", blobPath);
        }
        else
        {
            var properties = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath, blobName);
            
            response.StatusCode = result;
            
            if (properties.properties == null) return;
            
            SetResponseHeaders(response, properties.properties);
        }
    }

    private void HandleDeleteBlobRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleDeleteBlobRequest), "Handling deleting blob {0}.", blobName);
        
        var result = _dataPlane.DeleteBlob(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath, blobName);

        if (result == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode = result;
        }
    }

    private void HandleGetBlobPropertiesRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleGetBlobPropertiesRequest), "Handling blob properties for {0}.", blobPath);
        
        var properties = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath, blobName);

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

    private void HandleUploadBlobRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName, Stream input,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleUploadBlobRequest), "Handling blob upload for {0}.", blobPath);
        
        var result = _dataPlane.PutBlob(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, blobPath, blobName, input);

        // TODO: The response must include the response headers from https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#response
        response.StatusCode = result.code;

        if (result.properties == null) return;

        SetResponseHeaders(response, result.properties);
    }

    private static void SetResponseHeaders(HttpResponseMessage response,
        BlobProperties properties)
    {
        var etag = properties.ETag.ToString();
        if (!etag.StartsWith('"') && !etag.EndsWith('"'))
        {
            // Note we're enclosing ETag header explicitly with double quotes to align it 
            // with RFC description stating this tag is "nn entity tag consists of an opaque quoted string, possibly prefixed by a weakness indicator"
            response.Headers.Add("ETag", $"\"{etag}\"");
        }
        
        // Adding `Last-Modified` directly as response header fail with an error stating
        // we're misusing that header. However, based on the behavior of Blob Storage SDK 
        // it looks like it expects that header to be part of the response headers, not response
        // content. For now, we can leave it as it is (as SDK fallbacks to ETag anyway),
        // but it may be worth considering adding that header without validation if possible.
        var emptyContent = new StringContent(string.Empty);
        emptyContent.Headers.LastModified = DateTimeOffset.Parse(properties.LastModified!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
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

    private void HandleListBlobsRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName, HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleListBlobsRequest), "Handling listing blobs for {0}/{1}.", storageAccountName, containerName);
        
        // TODO: The request may come with additional keys in the query string, e.g.:
        // ?restype=container&comp=list&prefix=localhost/eh-test/$default/ownership/&include=Metadata  
        // We need to handle them as well

        var blobs = _dataPlane.ListBlobs(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(BlobEnumerationResult));
        serializer.Serialize(sw, blobs);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetContainersRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleGetContainersRequest), "Handling listing containers for {0}.", storageAccountName);
        
        var containers = _controlPlane.ListContainers(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(ContainerEnumerationResult));
        serializer.Serialize(sw, containers);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateContainerRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(HandleCreateContainerRequest), "Creating container: {0}", containerName);
        
        var code = _controlPlane.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, containerName, storageAccountName);

        response.StatusCode = code;
    }

    private string GetContainerName(string path)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(GetContainerName), "Looking for container name in {0}", path);

        var pathParts = path.Split('/');

        logger.LogDebug(nameof(BlobEndpoint), nameof(GetContainerName), "Returning: {0}", pathParts[1]);

        return pathParts[1];
    }

    private bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        logger.LogDebug(nameof(BlobEndpoint), nameof(TryGetStorageAccount), "Trying to get storage account.");

        if (!headers.TryGetValue("Host", out var host))
        {
            logger.LogError("`Host` header not found - it's required for storage account creation.");
            
            storageAccount = null;
            return false;
        }
        
        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];
        
        logger.LogDebug(nameof(BlobEndpoint), nameof(TryGetStorageAccount), "About to check if storage account '{0}' exists.", accountName);

        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, accountName!);
        if (identifiers != null)
        {
            storageAccount = _resourceProvider.GetAs<StorageAccountResource>(
                SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), accountName);
            return true;
        }

        storageAccount = null;
        logger.LogDebug(nameof(BlobEndpoint), nameof(TryGetStorageAccount), "Storage account '{0}' doesn't exists.", accountName);
        
        return false;
    }
}