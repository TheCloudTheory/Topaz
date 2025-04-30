using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Storage;

public partial class TableEndpoint : IEndpointDefinition
{
    private readonly TableServiceControlPlane controlPlane;

    public Protocol Protocol => Protocol.Http;

    public string DnsName => "/storage/{storageAccountName}/table";

    public TableEndpoint()
    {
        this.controlPlane = new TableServiceControlPlane();
    }

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers)
    {
        var response = new HttpResponseMessage();
        
        if(StorageAccountExists(path) == false)
        {
            response.StatusCode = System.Net.HttpStatusCode.NotFound;
            return response;
        }

        try
        {
            if (method == "GET")
            {
                switch (path)
                {
                    case "/Tables":
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
                switch (path)
                {
                    case "/Tables":
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
                    var matches = Regex.Match(path, @"^/Tables\('.*?'\)$", RegexOptions.IgnoreCase);
                    if(matches.Length == 0)
                    {
                        throw new Exception($"Invalid request path {path} for the delete operation.");
                    }

                    var tableName = matches.Value.Trim('/').Replace("Tables('", "").Replace("')", "");
                    PrettyLogger.LogDebug($"Attempting to delete table: {tableName}.");

                    this.controlPlane.DeleteTable(tableName);

                    PrettyLogger.LogDebug($"Table {tableName} deleted.");

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
            PrettyLogger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            return response;
        }

        throw new NotSupportedException();
    }

    private bool StorageAccountExists(string path)
    {
        throw new NotImplementedException();
    }

    private class TableEndpointResponse(TableProperties[] tables)
    {
        public TableProperties[] Value { get; init; } = tables;
    }
}
