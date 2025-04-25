using System.Net.Http.Json;
using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class TableEndpoint : IEndpointDefinition
{
    private readonly TableServiceControlPlane controlPlane;

    public Protocol Protocol => Protocol.Http;

    public string DnsName => "table.storage";

    public TableEndpoint()
    {
        this.controlPlane = new TableServiceControlPlane();
    }

    public HttpResponseMessage GetResponse(string path, Stream input)
    {
        var response = new HttpResponseMessage();

        switch (path)
        {
            case "/Tables":
                var tables = this.controlPlane.GetTables();
                response.Content = JsonContent.Create(tables);
                break;
            default:
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
                break;
        }
            
        return response;
    }
}
