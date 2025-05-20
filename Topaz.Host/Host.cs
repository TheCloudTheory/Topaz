using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Service.KeyVault;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Topaz.Host.AMQP;
using Topaz.Service.EventHub;
using Topaz.Service.Storage.Services;

namespace Topaz.Host;

public class Host(ILogger logger)
{
    private static readonly List<Thread> threads = [];
    private readonly ILogger logger = logger;

    public void Start()
    {
        var services = new IServiceDefinition[] {
            new AzureStorageService(this.logger),
            new TableStorageService(this.logger),
            new ResourceGroupService(this.logger),
            new SubscriptionService(this.logger),
            new KeyVaultService(this.logger),
            new EventHubService(this.logger),
            new BlobStorageService(this.logger)
        };
        
        var httpEndpoints = new List<IEndpointDefinition>();
        var amqpEndpoints = new List<IEndpointDefinition>();

        ExtractEndpointsForProtocols(services, httpEndpoints, [Protocol.Http, Protocol.Https]);
        ExtractEndpointsForProtocols(services, amqpEndpoints, [Protocol.Amqp]);

        CreateWebserverForHttpEndpoints([.. httpEndpoints]);
        CreateAmqpListenersForAmpqEndpoints([.. amqpEndpoints]);
    }

    private void CreateAmqpListenersForAmpqEndpoints(IEndpointDefinition[] endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var address = new Address($"amqp://localhost:{endpoint.PortAndProtocol.Port}");
            var listener = new ContainerHost(address);
            
            // TODO: Support other authentication mechanism besides CBS
            listener.Listeners[0].SASL.EnableMechanism(new Symbol("MSSBCBS"), new TopazSaslProfile(new Symbol("MSSBCBS")));
            listener.Listeners[0].AMQP.MaxFrameSize = 262144;
            
            listener.RegisterRequestProcessor("$cbs", new CbsProcessor());
            listener.RegisterLinkProcessor(new LinkProcessor());

            // Frame traces should be enabled only if LogLevel is set to Debug
            if (this.logger.LogLevel == LogLevel.Debug)
            {
                Trace.TraceLevel = TraceLevel.Frame;
                Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
            }
            
            threads.Add(new Thread(() => listener.Open()));
            threads.Last().Start();
        }
    }

    private void ExtractEndpointsForProtocols(IServiceDefinition[] services, List<IEndpointDefinition> httpEndpoints, Protocol[] protocols)
    {
        foreach (var service in services)
        {
            this.logger.LogDebug($"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                this.logger.LogDebug($"Processing {service.Name} endpoints...");

                if (protocols.Contains(endpoint.PortAndProtocol.Protocol) == false) continue;
                
                this.logger.LogDebug($"Processing {endpoint.PortAndProtocol} endpoint...");
                httpEndpoints.Add(endpoint);
            }
        }
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
                            break;
                        case Protocol.Https:
                            options.Listen(IPAddress.Loopback, httpEndpoint.PortAndProtocol.Port, listenOptions =>
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                {
                                    listenOptions.UseHttps("localhost.pfx", "qwerty");
                                }
                                else
                                {
                                    var certPem = File.ReadAllText("localhost.crt");
                                    var keyPem = File.ReadAllText("localhost.key");
                                    var x509 = X509Certificate2.CreateFromPem(certPem, keyPem);
                                    
                                    listenOptions.UseHttps(x509);
                                }
                            });

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    usedPorts.Add(httpEndpoint.PortAndProtocol.Port);
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
                        var port = context.Request.Host.Port;

                        if (method == null)
                        {
                            this.logger.LogDebug($"Received request with no method.");

                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return;
                        }

                        this.logger.LogDebug($"Received request: {method} {context.Request.Host}{path}{query}");

                        IEndpointDefinition? endpoint = null;
                        var pathParts = path.Split('/');
                        foreach (var httpEndpoint in httpEndpoints.Where(e => e.PortAndProtocol.Port == port))
                        {
                            foreach (var endpointUrl in httpEndpoint.Endpoints)
                            {
                                var methodAndPath = endpointUrl.Split(" ");
                                var endpointMethod = methodAndPath[0];
                                var endpointPath = methodAndPath[1];
                                
                                if(method != endpointMethod) continue;
                                
                                var endpointParts = endpointPath.Split('/');
                                if (endpointParts.Length != pathParts.Length) continue;

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
                            context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
                        }

                        if (response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            this.logger.LogError(textResponse);
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
