using System.Net;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Host;

public class Host(ILogger logger)
{
    private const int hostPortNumber = 8899;
    private static readonly List<Thread> threads = [];
    private readonly ILogger logger = logger;

    public void Start()
    {
        var services = new[] { new AzureStorageService(this.logger) };
        var urls = new List<string>();
        var httpEndpoints = new List<IEndpointDefinition>();

        foreach (var service in services)
        {
            this.logger.LogDebug($"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                this.logger.LogDebug($"Processing {endpoint.DnsName} endpoint...");

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
                    try
                    {
                        var hostWithoutPort = context.Request.Host.Host.ToString();
                        var path = context.Request.Path.ToString();
                        var method = context.Request.Method;
                        var query = context.Request.QueryString;

                        if (method == null)
                        {
                            this.logger.LogDebug($"Received request with no method.");

                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return;
                        }

                        this.logger.LogDebug($"Received request: {method} {context.Request.Host}{path}{query}");

                        IEndpointDefinition? endpoint = null;
                        var pathParts = path.Split('/');
                        foreach(var httpEndpoint in httpEndpoints)
                        {
                            var endpointParts = httpEndpoint.DnsName.Split('/');

                            if(endpointParts.Length > pathParts.Length) continue;

                            for(var i = 0; i < endpointParts.Length; i++)
                            {
                                if(endpointParts[i].StartsWith('{') && endpointParts[i].EndsWith('}')) continue;
                                if(endpointParts[i] != pathParts[i]) continue;

                                endpoint = httpEndpoint;
                            }

                            if(endpoint != null) break;
                        }

                        if (endpoint == null)
                        {
                            this.logger.LogDebug($"Request {method} {path} has no corresponding endpoint assigned.");

                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }

                        var response = endpoint.GetResponse(path, method, context.Request.Body, context.Request.Headers, query);
                        var textResponse = await response.Content.ReadAsStringAsync();

                        this.logger.LogDebug($"Response: [{response.StatusCode}] [{path}] {textResponse}");

                        context.Response.StatusCode = (int)response.StatusCode;
                        await context.Response.WriteAsync(textResponse);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        this.logger.LogError(ex);

                        await context.Response.WriteAsync(ex.Message);
                    }
                });
            })
            .Build();

        threads.Add(new Thread(() => host.Run()));
        threads.Last().Start();
    }
}
