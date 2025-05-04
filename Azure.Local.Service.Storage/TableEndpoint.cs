using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Storage;

public partial class TableEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly TableServiceControlPlane controlPlane = new(logger);
    private readonly ResourceProvider resourceProvider = new(logger);
    private readonly ILogger logger = logger;

    public Protocol Protocol => Protocol.Http;

    public string DnsName => "/storage/{storageAccountName}/table";

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}");

        var response = new HttpResponseMessage();
        
        if(StorageAccountExists(path) == false)
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
                        var tables = this.controlPlane.GetTables(input);
                        var endpointResponse = new TableEndpointResponse(tables);
                        response.Content = JsonContent.Create(endpointResponse);
                        break;
                    default:
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
                            var tables = this.controlPlane.CreateTable(input);
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
                            var error = new ErrorResponse("EntityAlreadyExists", "Table already exists.");

                            response.StatusCode = System.Net.HttpStatusCode.Conflict;
                            response.Headers.Add("x-ms-error-code", "EntityAlreadyExists");
                            response.Content = JsonContent.Create(error);

                        }

                        break;
                    default:
                        response.StatusCode = System.Net.HttpStatusCode.NotFound;
                        break;
                }

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

                    this.controlPlane.DeleteTable(tableName);

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

    private string ClearOriginalPath(string path)
    {
        this.logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: {path}");

        var pathParts = path.Split('/');
        var newPath = string.Join('/', pathParts.Skip(3));

        this.logger.LogDebug($"Executing {nameof(ClearOriginalPath)}: New path: {newPath}");

        return newPath;
    }

    private bool StorageAccountExists(string path)
    {
        this.logger.LogDebug($"Executing {nameof(StorageAccountExists)}: {path}");

        var pathParts = path.Split('/');
        var accountName = pathParts[2];

        this.logger.LogDebug($"About to check if storage account '{accountName}' exists.");

        return this.resourceProvider.CheckIfStorageAccountExists(accountName);
    }

    private class TableEndpointResponse(TableProperties[] tables)
    {
        public TableProperties[] Value { get; init; } = tables;
    }
}
