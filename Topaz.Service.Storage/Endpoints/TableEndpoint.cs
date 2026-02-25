using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Services;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints;

public class TableEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane _controlPlane = new(new TableResourceProvider(logger), logger);
    private readonly TableServiceDataPlane _dataPlane = new(new TableResourceProvider(logger), logger);
    private readonly ResourceProvider _resourceProvider = new(logger);
    private readonly TableStorageSecurityProvider _securityProvider = new(logger);

    public string[] Endpoints =>
    [
        "GET /Tables",
        "POST /Tables",
        @"DELETE /^Tables\('.*?'\)$",
        "POST /{tableName}",
        "PUT /{tableName}",
        "GET /{tableName}",
        "GET /",
        "PUT /{tableName}",
        @"PATCH /^.*?\(PartitionKey='.*?',RowKey='.*?'\)$",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Http);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!_securityProvider.RequestIsAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                context.Request.Headers, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        try
        {
            switch (context.Request.Method)
            {
                case "GET":
                    switch (context.Request.Path)
                    {
                        case "/":
                            HandleGetTablePropertiesRequest(subscriptionIdentifier, resourceGroupIdentifier,
                                storageAccount.Name, response, context.Request.QueryString);
                            break;
                        case "/Tables":
                            HandleGetTablesRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                                response);
                            break;
                        default:
                            if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "acl")
                            {
                                HandleGetAclRequest(subscriptionIdentifier, resourceGroupIdentifier,
                                    storageAccount.Name, context.Request.Path, response);
                                return;
                            }

                            var potentialTableName = context.Request.Path.Value.Replace("()", string.Empty).Replace("/", string.Empty);
                            if (IsPathReferencingTable(subscriptionIdentifier, resourceGroupIdentifier,
                                    potentialTableName, storageAccount.Name))
                            {
                                var entities = _dataPlane.QueryEntities(context.Request.QueryString, subscriptionIdentifier,
                                    resourceGroupIdentifier, potentialTableName, storageAccount.Name);
                                var dataEndpointResponse = new TableDataEndpointResponse(entities);

                                response.Content = JsonContent.Create(dataEndpointResponse);
                                response.StatusCode = HttpStatusCode.OK;

                                break;
                            }

                            response.StatusCode = HttpStatusCode.NotFound;
                            break;
                    }

                    return;
                case "POST":
                    switch (context.Request.Path)
                    {
                        case "/Tables":
                            try
                            {
                                HandleCreateTable(context.Request.Body, context.Request.Headers, subscriptionIdentifier, resourceGroupIdentifier,
                                    storageAccount.Name, response);
                            }
                            catch (EntityAlreadyExistsException)
                            {
                                var error = new TableErrorResponse("TableAlreadyExists", "Table already exists.");

                                response.StatusCode = HttpStatusCode.Conflict;
                                response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
                                response.Content = JsonContent.Create(error);
                            }

                            break;
                        default:
                            if (IsPathReferencingTable(subscriptionIdentifier, resourceGroupIdentifier, context.Request.Path,
                                    storageAccount.Name))
                            {
                                try
                                {
                                    var tableName = context.Request.Path.Value.Replace("/", string.Empty);
                                    var payload = _dataPlane.InsertEntity(context.Request.Body, subscriptionIdentifier,
                                        resourceGroupIdentifier, tableName, storageAccount.Name);

                                    // Depending on the value of the `Prefer` header, the response 
                                    // given by the emulator should be either 204 or 201
                                    if (!context.Request.Headers.TryGetValue("Prefer", out var prefer) || prefer != "return-no-content")
                                    {
                                        // No `Prefer` header or value other than `return-no-content`
                                        // hence the result will be 201
                                        response.StatusCode = HttpStatusCode.Created;
                                        response.Content = JsonContent.Create(payload);
                                    }

                                    if (prefer == "return-no-content")
                                    {
                                        response.StatusCode = HttpStatusCode.NoContent;
                                    }

                                    break;
                                }
                                catch (EntityAlreadyExistsException)
                                {
                                    var error = new TableErrorResponse("EntityAlreadyExists", "Entity already exists.");

                                    response.StatusCode = HttpStatusCode.Conflict;
                                    response.Headers.Add("x-ms-error-code", "EntityAlreadyExists");
                                    response.Content = JsonContent.Create(error);

                                    break;
                                }
                            }

                            var matches = Regex.Match(context.Request.Path, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$",
                                RegexOptions.IgnoreCase);
                            if (matches.Length > 0)
                            {
                                logger.LogDebug(nameof(TableEndpoint), nameof(GetResponse),
                                    "Matched the update operation.");

                                var (tableName, partitionKey, rowKey) = GetOperationDataForUpdateOperation(matches);

                                try
                                {
                                    _dataPlane.UpdateEntity(context.Request.Body, subscriptionIdentifier, resourceGroupIdentifier,
                                        tableName, storageAccount.Name, partitionKey, rowKey, context.Request.Headers);

                                    response.StatusCode = HttpStatusCode.NoContent;
                                }
                                catch (EntityNotFoundException)
                                {
                                    var error = new TableErrorResponse("EntityNotFound", "Entity not found.");

                                    response.StatusCode = HttpStatusCode.NotFound;
                                    response.Headers.Add("x-ms-error-code", "EntityNotFound");
                                    response.Content = JsonContent.Create(error);
                                }
                                catch (UpdateConditionNotSatisfiedException)
                                {
                                    var error = new TableErrorResponse("UpdateConditionNotSatisfied",
                                        "The update condition specified in the request was not satisfied.");

                                    response.StatusCode = HttpStatusCode.PreconditionFailed;
                                    response.Headers.Add("x-ms-error-code", "UpdateConditionNotSatisfied");
                                    response.Content = JsonContent.Create(error);
                                }

                                break;
                            }

                            response.StatusCode = HttpStatusCode.NotFound;
                            break;
                    }

                    return;
                case "PUT":
                case "PATCH":
                {
                    var matches = Regex.Match(context.Request.Path, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$",
                        RegexOptions.IgnoreCase);
                    if (matches.Length > 0)
                    {
                        HandleUpdateEntityRequest(context.Request.Body, context.Request.Headers, matches, subscriptionIdentifier,
                            resourceGroupIdentifier, storageAccount.Name, response);
                        return;
                    }

                    if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "acl")
                    {
                        var tableName = context.Request.Path.Value.Replace("/", string.Empty);
                        HandleSetAclRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                            tableName, context.Request.Body, response);
                        return;
                    }

                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                }
                case "DELETE":
                    try
                    {
                        var matches = Regex.Match(context.Request.Path, @"^\/Tables\('.*?'\)$", RegexOptions.IgnoreCase);
                        if (matches.Length == 0)
                        {
                            throw new Exception($"Invalid request path {context.Request.Path} for the delete operation.");
                        }

                        var tableName = matches.Value.Trim('/').Replace("Tables('", "").Replace("')", "");

                        logger.LogDebug(nameof(TableEndpoint), nameof(GetResponse), "Attempting to delete table: {0}.",
                            tableName);
                        _controlPlane.DeleteTable(subscriptionIdentifier, resourceGroupIdentifier, tableName,
                            storageAccount.Name);
                        logger.LogDebug(nameof(TableEndpoint), nameof(GetResponse), "Table {0} deleted.", tableName);

                        response.StatusCode = HttpStatusCode.NoContent;

                        return;
                    }
                    catch (EntityNotFoundException)
                    {
                        var error = new TableErrorResponse("EntityNotFound", "Table not found.");

                        response.StatusCode = HttpStatusCode.NotFound;
                        response.Headers.Add("x-ms-error-code", "EntityNotFound");
                        response.Content = JsonContent.Create(error);

                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return;
        }

        throw new NotSupportedException();
    }

    private void HandleCreateTable(Stream input, IHeaderDictionary headers,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        HttpResponseMessage response)
    {
        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateTableRequest>(rawContent, GlobalSettings.JsonOptions);

        if (request == null || string.IsNullOrEmpty(request.TableName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var tableExists = _controlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, request.TableName);
        if (tableExists)
        {
            var error = new TableErrorResponse("TableAlreadyExists", "Table already exists.");

            response.StatusCode = HttpStatusCode.Conflict;
            response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
            response.Content = JsonContent.Create(error);

            return;
        }

        var table = _controlPlane.CreateTable(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            request);

        // Depending on the value of the `Prefer` header, the response 
        // given by the emulator should be either 204 or 201. The header
        // is also instructing the server to ignore creating a table if
        // it already exists
        if (!headers.TryGetValue("Prefer", out var prefer) || prefer != "return-no-content")
        {
            response.Content = JsonContent.Create(table);

            // No `Prefer` header or value other than `return-no-content`
            // hence the result will be 201
            response.StatusCode = HttpStatusCode.Created;
            response.Headers.Add("Preference-Applied", "return-content");
        }

        if (prefer != "return-no-content") return;

        response.StatusCode = HttpStatusCode.NoContent;
        response.Headers.Add("Preference-Applied", "return-no-content");
    }

    private void HandleGetAclRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string path,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(HandleGetAclRequest), "Executing {0}.",
            nameof(HandleGetAclRequest));

        var tableName = path.Replace("/", string.Empty);
        var acls = _controlPlane.GetAcl(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(SignedIdentifiers));
        serializer.Serialize(sw, acls);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleSetAclRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName, Stream input,
        HttpResponseMessage response)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(HandleSetAclRequest), "Executing {0}.",
            nameof(HandleSetAclRequest));

        var code = _controlPlane.SetAcl(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName,
            input);
        response.StatusCode = code;
    }

    private void HandleUpdateEntityRequest(Stream input, IHeaderDictionary headers, Match matches,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, HttpResponseMessage response)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(HandleUpdateEntityRequest), "Matched the update operation.");

        var (tableName, partitionKey, rowKey) = GetOperationDataForUpdateOperation(matches);

        try
        {
            _dataPlane.UpdateEntity(input, subscriptionIdentifier, resourceGroupIdentifier, tableName,
                storageAccountName, partitionKey, rowKey, headers);

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (EntityNotFoundException)
        {
            var error = new TableErrorResponse("EntityNotFound", "Entity not found.");

            response.StatusCode = HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "EntityNotFound");
            response.Content = JsonContent.Create(error);
        }
        catch (UpdateConditionNotSatisfiedException)
        {
            var error = new TableErrorResponse("UpdateConditionNotSatisfied",
                "The update condition specified in the request was not satisfied.");

            response.StatusCode = HttpStatusCode.PreconditionFailed;
            response.Headers.Add("x-ms-error-code", "UpdateConditionNotSatisfied");
            response.Content = JsonContent.Create(error);
        }
    }

    private void HandleGetTablePropertiesRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, HttpResponseMessage response,
        QueryString query)
    {
        ThrowIfGetPropertiesRequestIsInvalid(query);

        var properties =
            _controlPlane.GetTableProperties(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableServiceProperties));
        serializer.Serialize(sw, properties);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void ThrowIfGetPropertiesRequestIsInvalid(QueryString query)
    {
        if (!query.HasValue) throw new Exception($"QueryString '{query}' is missing.");

        var collection = HttpUtility.ParseQueryString(query.Value);
        if (!collection.AllKeys.Contains("restype") || !collection.AllKeys.Contains("comp"))
            throw new Exception("Query string is missing required fields.");

        var restype = collection["restype"];
        var comp = collection["comp"];

        if (restype != "service") throw new Exception("Invalid value for 'restype'.");
        if (comp != "properties") throw new Exception("Invalid value for 'comp'.");
    }

    private void HandleGetTablesRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, HttpResponseMessage response)
    {
        var tables = _controlPlane.GetTables(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var endpointResponse = new TableEndpointResponse(tables);
        response.Content = JsonContent.Create(endpointResponse);
    }

    private (string TableName, string PartitionKey, string RowKey) GetOperationDataForUpdateOperation(Match matches)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(GetOperationDataForUpdateOperation), "Executing {0}: {1}",
            nameof(GetOperationDataForUpdateOperation), matches);

        var match = matches.Value;
        var dataMatches = Regex.Match(match,
            @"^(?<tableName>\w+)\(PartitionKey='(?<partitionKey>\w+)',RowKey='(?<rowKey>\w+)'\)$");

        var tableName = dataMatches.Groups["tableName"].Value;
        var partitionKey = dataMatches.Groups["partitionKey"].Value;
        var rowKey = dataMatches.Groups["rowKey"].Value;

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
        {
            throw new InvalidInputException();
        }

        return (TableName: tableName, PartitionKey: partitionKey, RowKey: rowKey);
    }

    private bool IsPathReferencingTable(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tablePath, string storageAccountName)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(IsPathReferencingTable), "Executing {0}: {1} {2}",
            nameof(IsPathReferencingTable), tablePath, storageAccountName);

        // Path may start with `/` so we need to get rid of it
        var tableName = tablePath.Replace("/", string.Empty);

        return _controlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            tableName);
    }

    private bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        logger.LogDebug(nameof(TableEndpoint), nameof(TryGetStorageAccount), "Executing {0}",
            nameof(TryGetStorageAccount));

        if (!headers.TryGetValue("Host", out var host))
        {
            logger.LogError("`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        logger.LogDebug(nameof(TableEndpoint), nameof(TryGetStorageAccount),
            "About to check if storage account '{0}' exists.", accountName);

        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, accountName!);
        if (identifiers != null)
        {
            storageAccount = _resourceProvider.GetAs<StorageAccountResource>(
                SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), accountName);
            return true;
        }

        storageAccount = null;
        return false;
    }

    private class TableEndpointResponse(TableProperties[] tables)
    {
        public TableProperties[] Value { get; init; } = tables;
    }

    private class TableDataEndpointResponse(object?[] values)
    {
        public object?[] Value { get; init; } = values;
    }
}