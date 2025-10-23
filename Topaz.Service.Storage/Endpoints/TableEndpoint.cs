using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints;

public class TableEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane _controlPlane = new(new TableResourceProvider(logger), logger);
    private readonly TableServiceDataPlane _dataPlane = new(new TableServiceControlPlane(new TableResourceProvider(logger), logger), logger);
    private readonly ResourceProvider _resourceProvider = new(logger);
    private readonly TableStorageSecurityProvider _securityProvider = new(logger); 

    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultTableStoragePort, Protocol.Http);

    public string[] Endpoints => [
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

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();
        
        if(!TryGetStorageAccount(headers, out var storageAccountName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }

        if (!_securityProvider.RequestIsAuthorized(null, null, storageAccountName.Name, headers, path, query))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return response;
        }

        try
        {
            if (method == "GET")
            {
                switch (path)
                {
                    case "/":
                        HandleGetTablePropertiesRequest(null, null, storageAccountName.Name, response, query);
                        break;
                    case "/Tables":
                        HandleGetTablesRequest(storageAccountName.Name, response);
                        break;
                    default:
                        if(query.TryGetValueForKey("comp", out var comp) && comp == "acl")
                        {
                            HandleGetAclRequest(storageAccountName.Name, path, response);
                            return response;
                        }
                        
                        var potentialTableName = path.Replace("()", string.Empty).Replace("/", string.Empty);
                        if(IsPathReferencingTable(potentialTableName, storageAccountName.Name))
                        {
                            var entities = _dataPlane.QueryEntities(query, potentialTableName, storageAccountName.Name);
                            var dataEndpointResponse = new TableDataEndpointResponse(entities);

                            response.Content = JsonContent.Create(dataEndpointResponse);
                            response.StatusCode = HttpStatusCode.OK;

                            break;
                        }

                        response.StatusCode = HttpStatusCode.NotFound;
                        break;
                }

                return response;
            }

            if (method == "POST")
            {
                switch (path)
                {
                    case "/Tables":
                        try
                        {
                            HandleCreateTable(input, headers, storageAccountName.Name, response);
                        }
                        catch (EntityAlreadyExistsException)
                        {
                            var error = new ErrorResponse("TableAlreadyExists", "Table already exists.");

                            response.StatusCode = HttpStatusCode.Conflict;
                            response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
                            response.Content = JsonContent.Create(error);
                        }

                        break;
                    default:
                        if(IsPathReferencingTable(path, storageAccountName.Name))
                        {
                            try
                            {
                                var tableName = path.Replace("/", string.Empty);
                                var payload = _dataPlane.InsertEntity(input, tableName, storageAccountName.Name);

                                // Depending on the value of the `Prefer` header, the response 
                                // given by the emulator should be either 204 or 201
                                if (headers.TryGetValue("Prefer", out var prefer) == false || prefer != "return-no-content")
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
                            catch(EntityAlreadyExistsException)
                            {
                                var error = new ErrorResponse("EntityAlreadyExists", "Entity already exists.");

                                response.StatusCode = HttpStatusCode.Conflict;
                                response.Headers.Add("x-ms-error-code", "EntityAlreadyExists");
                                response.Content = JsonContent.Create(error);

                                break;
                            }
                        }

                        var matches = Regex.Match(path, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$", RegexOptions.IgnoreCase);
                        if(matches.Length > 0)
                        {
                            logger.LogDebug("Matched the update operation.");

                            var (TableName, PartitionKey, RowKey) = GetOperationDataForUpdateOperation(matches);

                            try
                            {
                                _dataPlane.UpdateEntity(input, TableName, storageAccountName.Name, PartitionKey, RowKey, headers);

                                response.StatusCode = HttpStatusCode.NoContent;
                            }
                            catch(EntityNotFoundException)
                            {
                                var error = new ErrorResponse("EntityNotFound", "Entity not found.");

                                response.StatusCode = HttpStatusCode.NotFound;
                                response.Headers.Add("x-ms-error-code", "EntityNotFound");
                                response.Content = JsonContent.Create(error);
                            }
                            catch(UpdateConditionNotSatisfiedException)
                            {
                                var error = new ErrorResponse("UpdateConditionNotSatisfied", "The update condition specified in the request was not satisfied.");

                                response.StatusCode = HttpStatusCode.PreconditionFailed;
                                response.Headers.Add("x-ms-error-code", "UpdateConditionNotSatisfied");
                                response.Content = JsonContent.Create(error);
                            }
                            
                            break;
                        }
                        
                        response.StatusCode = HttpStatusCode.NotFound;
                        break;
                }

                return response;
            }

            if(method == "PUT" || method == "PATCH")
            {
                var matches = Regex.Match(path, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$", RegexOptions.IgnoreCase);
                if(matches.Length > 0)
                {
                    HandleUpdateEntityRequest(input, headers, matches, storageAccountName.Name, response);
                    return response;
                }
                
                if(query.TryGetValueForKey("comp", out var comp) && comp == "acl")
                {
                    var tableName = path.Replace("/", string.Empty);
                    HandleSetAclRequest(storageAccountName.Name, tableName, input, response);
                    return response;
                }
                
                response.StatusCode = HttpStatusCode.NotFound;
                return response;
            }

            if(method == "DELETE")
            {
                try
                {
                    var matches = Regex.Match(path, @"^\/Tables\('.*?'\)$", RegexOptions.IgnoreCase);
                    if(matches.Length == 0)
                    {
                        throw new Exception($"Invalid request path {path} for the delete operation.");
                    }

                    var tableName = matches.Value.Trim('/').Replace("Tables('", "").Replace("')", "");

                    logger.LogDebug($"Attempting to delete table: {tableName}.");
                    _controlPlane.DeleteTable(tableName, storageAccountName.Name);
                    logger.LogDebug($"Table {tableName} deleted.");

                    response.StatusCode = HttpStatusCode.NoContent;

                    return response;
                }
                catch (EntityNotFoundException)
                {
                    var error = new ErrorResponse("EntityNotFound", "Table not found.");

                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Headers.Add("x-ms-error-code", "EntityNotFound");
                    response.Content = JsonContent.Create(error);

                    return response;
                }
            }
  
        }
        catch (Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }

        throw new NotSupportedException();
    }

    private void HandleCreateTable(Stream input, IHeaderDictionary headers, string storageAccountName,
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
        
        var tableExists = _controlPlane.CheckIfTableExists(storageAccountName, request.TableName);
        if (tableExists)
        {
            var error = new ErrorResponse("TableAlreadyExists", "Table already exists.");

            response.StatusCode = HttpStatusCode.Conflict;
            response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
            response.Content = JsonContent.Create(error);

            return;
        }
        
        var table = _controlPlane.CreateTable(storageAccountName, request);
        
        // Depending on the value of the `Prefer` header, the response 
        // given by the emulator should be either 204 or 201. The header
        // is also instructing the server to ignore creating a table if
        // it already exists
        if (headers.TryGetValue("Prefer", out var prefer) == false || prefer != "return-no-content")
        {
            response.Content = JsonContent.Create(table);
            
            // No `Prefer` header or value other than `return-no-content`
            // hence the result will be 201
            response.StatusCode = HttpStatusCode.Created;
            response.Headers.Add("Preference-Applied", "return-content");
        }

        if (prefer == "return-no-content")
        {
            response.StatusCode = HttpStatusCode.NoContent;
            response.Headers.Add("Preference-Applied", "return-no-content");
        }
    }

    private void HandleGetAclRequest(string storageAccountName, string path, HttpResponseMessage response)
    {
        logger.LogDebug($"Executing {nameof(HandleGetAclRequest)}.");

        var tableName = path.Replace("/", string.Empty);
        var acls = _controlPlane.GetAcl(storageAccountName, tableName);
        
        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(SignedIdentifiers));
        serializer.Serialize(sw, acls);
        
        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleSetAclRequest(string storageAccountName, string tableName, Stream input, HttpResponseMessage response)
    {
        logger.LogDebug($"Executing {nameof(HandleSetAclRequest)}.");
        
        var code = _controlPlane.SetAcl(storageAccountName, tableName, input);
        response.StatusCode = code;
    }

    private void HandleUpdateEntityRequest(Stream input, IHeaderDictionary headers, Match matches,
        string storageAccountName, HttpResponseMessage response)
    {
        logger.LogDebug("Matched the update operation.");

        var (tableName, partitionKey, rowKey) = GetOperationDataForUpdateOperation(matches);

        try
        {
            this._dataPlane.UpdateEntity(input, tableName, storageAccountName, partitionKey, rowKey, headers);

            response.StatusCode = System.Net.HttpStatusCode.NoContent;
        }
        catch(EntityNotFoundException)
        {
            var error = new ErrorResponse("EntityNotFound", "Entity not found.");

            response.StatusCode = System.Net.HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "EntityNotFound");
            response.Content = JsonContent.Create(error);
        }
        catch(UpdateConditionNotSatisfiedException)
        {
            var error = new ErrorResponse("UpdateConditionNotSatisfied", "The update condition specified in the request was not satisfied.");

            response.StatusCode = System.Net.HttpStatusCode.PreconditionFailed;
            response.Headers.Add("x-ms-error-code", "UpdateConditionNotSatisfied");
            response.Content = JsonContent.Create(error);
        }
    }

    private void HandleGetTablePropertiesRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, HttpResponseMessage response,
        QueryString query)
    {
        ThrowIfGetPropertiesRequestIsInvalid(query);

        var properties = _controlPlane.GetTableProperties(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableServiceProperties));
        serializer.Serialize(sw, properties);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void ThrowIfGetPropertiesRequestIsInvalid(QueryString query)
    {
        if(!query.HasValue) throw new Exception($"QueryString '{query}' is missing.");
        
        var collection = HttpUtility.ParseQueryString(query.Value);
        if (!collection.AllKeys.Contains("restype") || !collection.AllKeys.Contains("comp"))
            throw new Exception("Query string is missing required fields.");
        
        var restype = collection["restype"];
        var comp = collection["comp"];
        
        if(restype != "service") throw new Exception("Invalid value for 'restype'.");
        if(comp != "properties") throw new Exception("Invalid value for 'comp'.");
    }

    private void HandleGetTablesRequest(string storageAccountName, HttpResponseMessage response)
    {
        var tables = this._controlPlane.GetTables(storageAccountName);
        var endpointResponse = new TableEndpointResponse(tables);
        response.Content = JsonContent.Create(endpointResponse);
    }

    private (string TableName, string PartitionKey, string RowKey) GetOperationDataForUpdateOperation(Match matches)
    {
        logger.LogDebug($"Executing {nameof(GetOperationDataForUpdateOperation)}: {matches}");

        var match = matches.Value;
        var dataMatches = Regex.Match(match, @"^(?<tableName>\w+)\(PartitionKey='(?<partitionKey>\w+)',RowKey='(?<rowKey>\w+)'\)$");

        var tableName = dataMatches.Groups["tableName"].Value;
        var partitionKey = dataMatches.Groups["partitionKey"].Value;
        var rowKey = dataMatches.Groups["rowKey"].Value;

        if(string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
        {
            throw new InvalidInputException();
        }

        return (TableName: tableName, PartitionKey: partitionKey, RowKey: rowKey);
    }

    private bool IsPathReferencingTable(string tablePath, string storageAccountName)
    {
        logger.LogDebug($"Executing {nameof(IsPathReferencingTable)}: {tablePath} {storageAccountName}");
        
        // Path may start with `/` so we need to get rid of it
        var tableName = tablePath.Replace("/", string.Empty);

        return _controlPlane.CheckIfTableExists(storageAccountName, tableName);
    }

    private string ClearOriginalPath(string path)
    {
        logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: {path}");

        var newPath = path.Split('/')[0];

        logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: New path: {newPath}");

        return newPath;
    }

    private bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        logger.LogDebug($"Executing {nameof(TryGetStorageAccount)}");

        if (!headers.TryGetValue("Host", out var host))
        {
            logger.LogError("`Host` header not found - it's required for storage account creation.");
            
            storageAccount = null;
            return false;
        }
        
        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        if (_resourceProvider.CheckIfStorageAccountExists(accountName))
        {
            storageAccount = _resourceProvider.GetAs<StorageAccountResource>(null, null, accountName);
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
