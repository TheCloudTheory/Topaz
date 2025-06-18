using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Topaz.CloudEnvironment;
using Topaz.Host.AMQP;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Host;

public class Host(GlobalOptions options, ITopazLogger logger)
{
    private static readonly List<Thread> Threads = [];

    public void Start()
    {
        Console.WriteLine("Topaz.Host - Welcome!");
        Console.WriteLine($"Version: {ThisAssembly.AssemblyInformationalVersion}");
        Console.WriteLine("==============================");
        
        var services = new IServiceDefinition[] {
            new AzureStorageService(logger),
            new TableStorageService(logger),
            new ResourceGroupService(logger),
            new SubscriptionService(logger),
            new KeyVaultService(logger),
            new EventHubService(logger),
            new BlobStorageService(logger),
            new TopazCloudEnvironmentService()
        };
        
        var httpEndpoints = new List<IEndpointDefinition>();
        var amqpEndpoints = new List<IEndpointDefinition>();

        ExtractEndpointsForProtocols(services, httpEndpoints, [Protocol.Http, Protocol.Https]);
        ExtractEndpointsForProtocols(services, amqpEndpoints, [Protocol.Amqp]);

        CreateWebserverForHttpEndpoints([.. httpEndpoints]);
        CreateAmqpListenersForAmpqEndpoints([.. amqpEndpoints]);

        Console.WriteLine("Enabled services:");
        
        foreach (var service in services)
        {
            Console.WriteLine($"- {service.Name}: {string.Join(", ", service.Endpoints.Select(e => $"{e.PortAndProtocol.Protocol} -> {e.PortAndProtocol.Port}"))}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Topaz.Host listening to incoming requests...");
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
            listener.RegisterRequestProcessor("$management", new ManagementProcessor());
            listener.RegisterLinkProcessor(new LinkProcessor(logger));

            // Frame traces should be enabled only if LogLevel is set to Debug
            if (logger.LogLevel == LogLevel.Debug)
            {
                Trace.TraceLevel = TraceLevel.Frame;
                Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
            }
            
            Threads.Add(new Thread(() => listener.Open()));
            Threads.Last().Start();
        }
    }

    private void ExtractEndpointsForProtocols(IServiceDefinition[] services, List<IEndpointDefinition> httpEndpoints, Protocol[] protocols)
    {
        foreach (var service in services)
        {
            logger.LogDebug($"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                logger.LogDebug($"Processing {service.Name} endpoints...");

                if (protocols.Contains(endpoint.PortAndProtocol.Protocol) == false) continue;
                
                logger.LogDebug($"Processing {endpoint.PortAndProtocol} endpoint...");
                httpEndpoints.Add(endpoint);
            }
        }
    }

    private void CreateWebserverForHttpEndpoints(IEndpointDefinition[] httpEndpoints)
    {
        var host = new WebHostBuilder()
            .UseKestrel((context, hostOptions) =>
            {
                var usedPorts = new List<int>();
                foreach (var httpEndpoint in httpEndpoints)
                {
                    if (usedPorts.Contains(httpEndpoint.PortAndProtocol.Port))
                    {
                        logger.LogDebug($"Using port {httpEndpoint.PortAndProtocol.Port} will be skipped as it's already registered.");
                        continue;
                    }

                    switch (httpEndpoint.PortAndProtocol.Protocol)
                    {
                        case Protocol.Http:
                            hostOptions.Listen(IPAddress.Any, httpEndpoint.PortAndProtocol.Port);
                            break;
                        case Protocol.Https:
                            hostOptions.Listen(IPAddress.Any, httpEndpoint.PortAndProtocol.Port, listenOptions =>
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                {
                                    listenOptions.UseHttps("localhost.pfx", "qwerty");
                                }
                                else
                                {
                                    string? certPem = null;
                                    string? keyPem = null;
                                    
                                    if (string.IsNullOrEmpty(options.CertificateFile) == false &&
                                        string.IsNullOrEmpty(options.CertificateKey) == false)
                                    {
                                        logger.LogInformation("Using provided certificate file instead of the default one.");
                                        
                                        certPem = File.ReadAllText(options.CertificateFile);
                                        keyPem = File.ReadAllText(options.CertificateKey);
                                    }
                                    else
                                    {
                                        certPem = File.ReadAllText("localhost.crt");
                                        keyPem = File.ReadAllText("localhost.key");
                                    }
                                    
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
            .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
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
                            logger.LogDebug($"Received request with no method.");

                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return;
                        }

                        logger.LogInformation($"Received request: {method} {context.Request.Host}{path}{query}");

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
                                if (endpointParts.Length != pathParts.Length && IsEndpointWithDynamicRouting(endpointParts) == false) continue;

                                if (IsEndpointWithDynamicRouting(endpointParts))
                                {
                                    foreach (var part in endpointParts)
                                    {
                                        if (part.StartsWith('{') && part.EndsWith('}')) continue;
                                        if (part.Equals("...")) endpoint = httpEndpoint;
                                    }
                                }
                                else
                                {
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
                                }

                                // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
                                if (endpoint != null) break;
                            }

                            // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
                            if (endpoint != null) break;
                        }

                        if (endpoint == null)
                        {
                            logger.LogError($"Request {method} {path} has no corresponding endpoint assigned.");

                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }

                        var response = endpoint.GetResponse(path, method, context.Request.Body, context.Request.Headers, query, options);
                        var textResponse = await response.Content.ReadAsStringAsync();

                        logger.LogInformation($"Response: [{response.StatusCode}] [{path}] {textResponse}");

                        context.Response.StatusCode = (int)response.StatusCode;

                        foreach (var header in response.Headers)
                        {
                            context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
                        }

                        if (response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            logger.LogError(textResponse);
                        }

                        if(response.StatusCode != HttpStatusCode.NoContent)
                        {
                            await context.Response.WriteAsync(textResponse);
                        }               
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        logger.LogError(ex);

                        await context.Response.WriteAsync(ex.Message);
                    }
                });
            })
            .Build();

        Threads.Add(new Thread(() => host.Run()));
        Threads.Last().Start();
    }

    /// <summary>
    /// Checks if the provided endpoint allows dynamic routing. Dynamic routing is a concept
    /// when endpoint accepts multiple paths which point to a specific resource. An example
    /// of such an endpoint is UploadBlob endpoint for Blob Storage where path will differ depending
    /// on the blob location.
    /// </summary>
    /// <param name="endpointParts">An array of parts of the endpoint.</param>
    /// <returns>True if dynamic routing is allowed.</returns>
    private bool IsEndpointWithDynamicRouting(string[] endpointParts)
    {
        return endpointParts.Contains("...");
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
        return matches.Success;
    }
}
