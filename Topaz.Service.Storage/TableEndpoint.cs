using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Storage;

public partial class TableEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane controlPlane = new(new TableResourceProvider(logger));
    private readonly TableServiceDataPlane dataPlane = new(new TableServiceControlPlane(new TableResourceProvider(logger)), logger);
    private readonly ResourceProvider resourceProvider = new(logger);
    private readonly ILogger logger = logger;

    public (int Port, Protocol Protocol) PortAndProtocol => (8890, Protocol.Https);

    public string[] Endpoints => [
        "/storage/{storageAccountName}/Tables",
        @"/storage/{storageAccountName}/^Tables\('.*?'\)$",
        "/storage/{storageAccountName}/{tableName}"
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

        var actualPath = ClearOriginalPath(path);

        try
        {
            if (method == "GET")
            {
                switch (actualPath)
                {
                    case "Tables":
                        var tables = this.controlPlane.GetTables(storageAccountName);
                        var endpointResponse = new TableEndpointResponse(tables);
                        response.Content = JsonContent.Create(endpointResponse);
                        break;
                    default:
                        var potentialTableName = actualPath.Replace("()", string.Empty);
                        if(IsPathReferencingTable(potentialTableName, storageAccountName))
                        {
                            var entities = this.dataPlane.QueryEntities(query, potentialTableName, storageAccountName);
                            var dataEndpointResponse = new TableDataEndpointResponse(entities);

                            response.Content = JsonContent.Create(dataEndpointResponse);
                            response.StatusCode = System.Net.HttpStatusCode.OK;

                            break;
                        }

                        response.StatusCode = System.Net.HttpStatusCode.NotFound;
                        break;
                }

                return response;
            }

            if (method == "POST")
            {
                switch (actualPath)
                {
                    case "Tables":
                        try
                        {
                            var tables = this.controlPlane.CreateTable(input, storageAccountName);
                            response.Content = JsonContent.Create(tables);

                            // Depending on the value of the `Prefer` header, the response 
                            // given by the emulator should be either 204 or 201
                            if (headers.TryGetValue("Prefer", out var prefer) == false || prefer != "return-no-content")
                            {
                                // No `Prefer` header or value other than `return-no-content`
                                // hence the result will be 201
                                response.StatusCode = System.Net.HttpStatusCode.Created;
                            }

                            if (prefer == "return-no-content")
                            {
                                response.StatusCode = System.Net.HttpStatusCode.NoContent;
                            }
                        }
                        catch (EntityAlreadyExistsException)
                        {
                            var error = new ErrorResponse("TableAlreadyExists", "Table already exists.");

                            response.StatusCode = System.Net.HttpStatusCode.Conflict;
                            response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
                            response.Content = JsonContent.Create(error);
                        }

                        break;
                    default:
                        if(IsPathReferencingTable(actualPath, storageAccountName))
                        {
                            try
                            {
                                var payload = this.dataPlane.InsertEntity(input, actualPath, storageAccountName);

                                // Depending on the value of the `Prefer` header, the response 
                                // given by the emulator should be either 204 or 201
                                if (headers.TryGetValue("Prefer", out var prefer) == false || prefer != "return-no-content")
                                {
                                    // No `Prefer` header or value other than `return-no-content`
                                    // hence the result will be 201
                                    response.StatusCode = System.Net.HttpStatusCode.Created;
                                    response.Content = JsonContent.Create(payload);
                                }

                                if (prefer == "return-no-content")
                                {
                                    response.StatusCode = System.Net.HttpStatusCode.NoContent;
                                }

                                break;
                            }
                            catch(EntityAlreadyExistsException)
                            {
                                var error = new ErrorResponse("EntityAlreadyExists", "Entity already exists.");

                                response.StatusCode = System.Net.HttpStatusCode.Conflict;
                                response.Headers.Add("x-ms-error-code", "EntityAlreadyExists");
                                response.Content = JsonContent.Create(error);

                                break;
                            }
                        }

                        var matches = Regex.Match(actualPath, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$", RegexOptions.IgnoreCase);
                        if(matches.Length > 0)
                        {
                            this.logger.LogDebug("Matched the update operation.");

                            var (TableName, PartitionKey, RowKey) = GetOperationDataForUpdateOperation(matches);

                            try
                            {
                                this.dataPlane.UpdateEntity(input, TableName, storageAccountName, PartitionKey, RowKey, headers);

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
                            
                            break;
                        }
                        
                        response.StatusCode = System.Net.HttpStatusCode.NotFound;
                        break;
                }

                return response;
            }

            if(method == "PUT")
            {
                var matches = Regex.Match(actualPath, @"\w+\(PartitionKey='\w+',RowKey='\w+'\)$", RegexOptions.IgnoreCase);
                if(matches.Length > 0)
                {
                    this.logger.LogDebug("Matched the update operation.");

                    var (TableName, PartitionKey, RowKey) = GetOperationDataForUpdateOperation(matches);

                    try
                    {
                        this.dataPlane.UpdateEntity(input, TableName, storageAccountName, PartitionKey, RowKey, headers);

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
                    
                    return response;
                }
                
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
                return response;
            }

            if(method == "DELETE")
            {
                try
                {
                    var matches = Regex.Match(actualPath, @"^Tables\('.*?'\)$", RegexOptions.IgnoreCase);
                    if(matches.Length == 0)
                    {
                        throw new Exception($"Invalid request path {actualPath} for the delete operation.");
                    }

                    var tableName = matches.Value.Trim('/').Replace("Tables('", "").Replace("')", "");

                    this.logger.LogDebug($"Attempting to delete table: {tableName}.");
                    this.controlPlane.DeleteTable(tableName, storageAccountName);
                    this.logger.LogDebug($"Table {tableName} deleted.");

                    response.StatusCode = System.Net.HttpStatusCode.NoContent;

                    return response;
                }
                catch (EntityNotFoundException)
                {
                    var error = new ErrorResponse("EntityNotFound", "Table not found.");

                    response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    response.Headers.Add("x-ms-error-code", "EntityNotFound");
                    response.Content = JsonContent.Create(error);

                    return response;
                }
            }
  
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            return response;
        }

        throw new NotSupportedException();
    }

    private (string TableName, string PartitionKey, string RowKey) GetOperationDataForUpdateOperation(Match matches)
    {
        this.logger.LogDebug($"Executing {nameof(GetOperationDataForUpdateOperation)}: {matches}");

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

    private bool IsPathReferencingTable(string tableName, string storageAccountName)
    {
        this.logger.LogDebug($"Executing {nameof(IsPathReferencingTable)}: {tableName} {storageAccountName}");

        return this.controlPlane.CheckIfTableExists(tableName, storageAccountName);
    }

    private string ClearOriginalPath(string path)
    {
        this.logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: {path}");

        var pathParts = path.Split('/');
        var newPath = string.Join('/', pathParts.Skip(3));

        this.logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: New path: {newPath}");

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

    private class TableEndpointResponse(TableProperties[] tables)
    {
        public TableProperties[] Value { get; init; } = tables;
    }

    private class TableDataEndpointResponse(object?[] values)
    {
        public object?[] Value { get; init; } = values;
    }
}
