using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Topaz.CloudEnvironment;
using Topaz.Host.AMQP;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Host;

public class Host(GlobalOptions options, ITopazLogger logger)
{
    /// <summary>
    /// IP address used by Topaz to listen to incoming requests. Note that address is controlled by the
    /// host itself while ports and protocols are the responsibility of appropriate services.
    /// </summary>
    private const string TopazIpAddress = "127.0.2.1";
    
    private static readonly List<Thread> Threads = [];

    private readonly Router _router = new(options, logger);
    private readonly DnsManager _dnsManager = new();

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
            new TopazCloudEnvironmentService(),
            new ServiceBusService(logger)
        };

        // Topaz requires elevated permissions to run as there may be operations (like modifying entries
        // in the hosts file), which will require them to function properly.
        if (!Environment.IsPrivilegedProcess && !options.SkipRegistrationOfDnsEntries)
        {
            Console.Error.WriteLine("Topaz.Host - Not Privileged! You must run Topaz with elevated permissions in order for it to work properly. If you want to run Topaz without elevated permissions, use `--skip-dns-registration` option and set it to `true`.");
            return;
        }

        if (!options.SkipRegistrationOfDnsEntries)
        {
            _dnsManager.ConfigureEntries();
        }
        else
        {
            Console.WriteLine("Registration of DNS entries is disabled. Make sure you've added those entries manually before running Topaz.");
        }
        
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
            var address = new Address($"amqp://{TopazIpAddress}:{endpoint.PortAndProtocol.Port}");
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
                Trace.TraceListener = (l, f, a) => logger.LogDebug(string.Format(f, a));
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
                            hostOptions.Listen(IPAddress.Parse(TopazIpAddress), httpEndpoint.PortAndProtocol.Port);
                            break;
                        case Protocol.Https:
                            hostOptions.Listen(IPAddress.Parse(TopazIpAddress), httpEndpoint.PortAndProtocol.Port, listenOptions =>
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                {
                                    listenOptions.UseHttps("topaz.pfx", "qwerty");
                                }
                                else
                                {
                                    ConfigurePemCertificate(listenOptions);
                                }
                            });

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    usedPorts.Add(httpEndpoint.PortAndProtocol.Port);
                }
            })
            // Used to disable the obsolete messages displayed by Kestrel when starting
            .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    try
                    {
                        await _router.MatchAndExecuteEndpoint(httpEndpoints, context).ConfigureAwait(false);
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

    private void ConfigurePemCertificate(ListenOptions listenOptions)
    {
        string? certPem;
        string? keyPem;
                                    
        if (string.IsNullOrEmpty(options.CertificateFile) == false &&
            string.IsNullOrEmpty(options.CertificateKey) == false)
        {
            logger.LogInformation("Using provided certificate file instead of the default one.");
                                        
            certPem = File.ReadAllText(options.CertificateFile);
            keyPem = File.ReadAllText(options.CertificateKey);
        }
        else
        {
            certPem = File.ReadAllText("topaz.crt");
            keyPem = File.ReadAllText("topaz.key");
        }
                                    
        var x509 = X509Certificate2.CreateFromPem(certPem, keyPem);

        listenOptions.UseHttps(x509);
    }
}
