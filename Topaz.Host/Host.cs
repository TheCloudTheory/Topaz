using System.Net;
using System.Text.RegularExpressions;
using Topaz.Service.KeyVault;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Topaz.Host;

public class Host(ILogger logger)
{
    private const int hostPortNumber = 8899;
    private const int hostPortNumberSecure = 8900;
    private static readonly List<Thread> threads = [];
    private readonly ILogger logger = logger;

    public void Start()
    {
        var services = new IServiceDefinition[] {
            new AzureStorageService(this.logger),
            new TableStorageService(this.logger),
            new ResourceGroupService(this.logger),
            new SubscriptionService(this.logger),
            new KeyVaultService(this.logger)
        };
        var httpEndpoints = new List<IEndpointDefinition>();

        foreach (var service in services)
        {
            this.logger.LogDebug($"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                this.logger.LogDebug($"Processing {endpoint.Endpoints} endpoint...");

                if (endpoint.PortAndProtocol.Protocol is Protocol.Http or Protocol.Https)
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
            .UseKestrel((context, options) =>
            {
                var usedPorts = new List<int>();
                foreach (var httpEndpoint in httpEndpoints)
                {
                    if (usedPorts.Contains(httpEndpoint.PortAndProtocol.Port))
                    {
                        this.logger.LogDebug($"Using port {httpEndpoint.PortAndProtocol.Port} will be skipped as it's already registered.");
                        continue;
                    }
                    
                    switch (httpEndpoint.PortAndProtocol.Protocol)
                    {
                        case Protocol.Http:
                            options.Listen(IPAddress.Loopback, httpEndpoint.PortAndProtocol.Port);
                            usedPorts.Add(httpEndpoint.PortAndProtocol.Port);
                            break;
                        case Protocol.Https:
                            options.Listen(IPAddress.Loopback, httpEndpoint.PortAndProtocol.Port, listenOptions =>
                            {
                                listenOptions.UseHttps("localhost.pfx", "qwerty");
                            });
                            usedPorts.Add(httpEndpoint.PortAndProtocol.Port);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            })
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    try
                    {
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
                        foreach (var httpEndpoint in httpEndpoints)
                        {
                            foreach (var endpointUrl in httpEndpoint.Endpoints)
                            {
                                var endpointParts = endpointUrl.Split('/');
                                if (endpointParts.Length > pathParts.Length) continue;

                                for (var i = 0; i < endpointParts.Length; i++)
                                {
                                    if (endpointParts[i].StartsWith('{') && endpointParts[i].EndsWith('}')) continue;
                                    if (MatchesRegexExpressionForEndpoint(endpointParts[i], pathParts[i])) continue;
                                    if (string.Equals(endpointParts[i], pathParts[i], StringComparison.InvariantCultureIgnoreCase) == false)
                                    {
                                        endpoint = null; // We need to reset the endpoint as it doesn't look correct now
                                        continue;
                                    }

                                    endpoint = httpEndpoint;
                                }

                                // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
                                if (endpoint != null) break;
                            }

                            // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
                            if (endpoint != null) break;
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

                        foreach (var header in response.Headers)
                        {
                            var value = new StringValues(header.Value.ToArray());
                            context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
                        }

                        if(response.StatusCode != HttpStatusCode.NoContent)
                        {
                            await context.Response.WriteAsync(textResponse);
                        }               
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

    /// <summary>
    /// Determines whether a given path segment matches a specified endpoint segment using a regular expression.
    /// </summary>
    /// <param name="endpointSegment">The endpoint segment, which may contain a regular expression.</param>
    /// <param name="pathSegment">The path segment to compare against the endpoint segment.</param>
    /// <returns>
    /// True if the path segment matches the endpoint segment's regular expression; otherwise, false.
    /// </returns>
    private bool MatchesRegexExpressionForEndpoint(string endpointSegment, string pathSegment)
    {
        if(string.IsNullOrEmpty(endpointSegment) || string.IsNullOrEmpty(pathSegment)) return false;
        if(endpointSegment.StartsWith('^') == false) return false;

        var matches = Regex.Match(pathSegment, endpointSegment, RegexOptions.IgnoreCase);
        if(matches.Success == false) return false;

        return true;
    }
}
