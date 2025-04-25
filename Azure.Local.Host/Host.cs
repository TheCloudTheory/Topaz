using System.Net;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Host;

public class Host
{
    private const int hostPortNumber = 8899;
    private static List<Thread> threads = [];

    public void Start()
    {
        var services = new[] { new AzureStorageService() };
        var urls = new List<string>();
        var httpEndpoints = new List<IEndpointDefinition>();

        foreach (var service in services)
        {
            PrettyLogger.LogDebug($"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                PrettyLogger.LogDebug($"Processing {endpoint.DnsName} endpoint...");

                if (endpoint.Protocol == Service.Shared.Protocol.Http || endpoint.Protocol == Service.Shared.Protocol.Https)
                {
                    httpEndpoints.Add(endpoint);
                }
            }
        }

        CreateWebserverForHttpEndpoints([.. httpEndpoints]);
    }

    private void CreateWebserverForHttpEndpoints(IEndpointDefinition[] httpEndpoints)
    {
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls($"http://azure.localhost:{hostPortNumber}")
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    var hostWithoutPort = context.Request.Host.Host.ToString();
                    var path = context.Request.Path.ToString();

                    PrettyLogger.LogDebug($"Received request: {context.Request.Host}{path}");

                    var endpoint = httpEndpoints.SingleOrDefault(e => hostWithoutPort.StartsWith(e.DnsName));
                    if(endpoint == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    var response = endpoint.GetResponse(path, context.Request.Body);
                    context.Response.StatusCode = (int)response.StatusCode;

                    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                });
            })
            .Build();

        threads.Add(new Thread(() => host.Run()));
        threads.Last().Start();
    }
}
