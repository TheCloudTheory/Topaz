using System.Net.Http.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Shared;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Storage;

public class TableEndpoint : IEndpointDefinition
{
    private readonly TableServiceControlPlane controlPlane;

    public Protocol Protocol => Protocol.Http;

    public string DnsName => "/storage/table";

    public TableEndpoint()
    {
        this.controlPlane = new TableServiceControlPlane();
    }

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers)
    {
        var response = new HttpResponseMessage();

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

    private class TableEndpointResponse(TableItem[] tables)
    {
        public TableItem[] Value { get; init; } = tables;
    }
}
