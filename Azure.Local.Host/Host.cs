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
                    var method = context.Request.Method;

                    if(method == null)
                    {
                        PrettyLogger.LogDebug($"Received request with no method.");

                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        return;
                    }

                    PrettyLogger.LogDebug($"Received request: {method} {context.Request.Host}{path}");

                    var endpoint = httpEndpoints.SingleOrDefault(e => path.StartsWith(e.DnsName));
                    if(endpoint == null)
                    {
                        PrettyLogger.LogDebug($"Request {method} {path} has no corresponding endpoint assigned.");

                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    var servicePath = path.Replace(endpoint.DnsName, string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    var response = endpoint.GetResponse(servicePath, method, context.Request.Body, context.Request.Headers);
                    var textResponse = await response.Content.ReadAsStringAsync();

                    PrettyLogger.LogDebug($"Response: [{response.StatusCode}] {textResponse}");

                    context.Response.StatusCode = (int)response.StatusCode;
                    await context.Response.WriteAsync(textResponse);
                });
            })
            .Build();

        threads.Add(new Thread(() => host.Run()));
        threads.Last().Start();
    }
}
