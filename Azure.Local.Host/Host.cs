using System.Net;
using System.Text.RegularExpressions;
using Azure.Local.Service.KeyVault;
using Azure.Local.Service.ResourceGroup;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage;
using Azure.Local.Service.Subscription;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Host;

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
            .UseKestrel((context, options) =>
            {
                options.Listen(IPAddress.Loopback, hostPortNumber);
                options.Listen(IPAddress.Loopback, hostPortNumberSecure, listenOptions =>
                {
                    listenOptions.UseHttps("localhost.pfx", "qwerty");
                });
            })
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
