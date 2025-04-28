using System.Net.Http.Json;
using Azure.Local.Service.Shared;
using Azure.Local.Shared;

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

    public HttpResponseMessage GetResponse(string path, string method, Stream input)
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
                        response.Content = JsonContent.Create(tables);
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
                        var tables = this.controlPlane.CreateTable(input);
                        response.Content = JsonContent.Create(tables);
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
}
