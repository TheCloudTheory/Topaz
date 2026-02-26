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
using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.Host.AMQP;
using Topaz.Service.Authorization;
using Topaz.Service.Entra;
using Topaz.Service.EventHub;
using Topaz.Service.Insights;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Services;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;

namespace Topaz.Host;

public class Host
{
    private static bool IsRunningInsideContainer()
    {
        var env = Environment.GetEnvironmentVariable("TOPAZ_CONTAINERIZED");
        return env == "true";
    }

    private static readonly List<Thread> Threads = [];
    
    private readonly Pipeline _eventPipeline;
    private readonly Router _router;

    /// <summary>
    /// IP address used by Topaz to listen to incoming requests. Note that address is controlled by the
    /// host itself while ports and protocols are the responsibility of appropriate services.
    /// </summary>
    private readonly string _topazIpAddress;

    private readonly GlobalOptions _options;
    private readonly ITopazLogger _logger;

    public Host(GlobalOptions options, ITopazLogger logger)
    {
        _options = options;
        _logger = logger;
        _eventPipeline = new Pipeline(logger);
        _router = new Router(_eventPipeline, options, logger);
        _topazIpAddress = IsRunningInsideContainer() ? "0.0.0.0" :
            string.IsNullOrWhiteSpace(options.EmulatorIpAddress) ? "127.0.0.1" : options.EmulatorIpAddress;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.Title = "--- Topaz.Host ---";
        
        Console.WriteLine("");
        Console.WriteLine("==============================");
        Console.WriteLine("Topaz.Host - Welcome!");
        Console.WriteLine($"Version: {ThisAssembly.AssemblyInformationalVersion}");
        Console.WriteLine("==============================");
        Console.WriteLine("");
        
        GlobalDnsEntries.ConfigureLogger(_logger);
        
        var idFactory = new CorrelationIdFactory();
        var services = new IServiceDefinition[] {
            new AzureStorageService(_logger),
            new TableStorageService(_logger),
            new ResourceGroupService(_eventPipeline, _logger),
            new SubscriptionService(_eventPipeline, _logger),
            new KeyVaultService(_eventPipeline, _logger),
            new EventHubService(_logger),
            new BlobStorageService(_logger),
            new TopazCloudEnvironmentService(_logger),
            new ServiceBusService(_logger),
            new ResourceManagerService(_eventPipeline, _logger, cancellationToken),
            new VirtualNetworkService(_eventPipeline, _logger),
            new ManagedIdentityService(_eventPipeline, _logger),
            new ResourceAuthorizationService(_logger),
            new ResourceGroupAuthorizationService(_logger),
            new RoleDefinitionService(_eventPipeline, _logger),
            new RoleAssignmentService(_eventPipeline, _logger),
            new InsightsService(_logger),
            new EntraService(_logger)
        };
        
        _logger.ConfigureIdFactory(idFactory);
        
        Console.WriteLine("Bootstrapping services...");
        foreach (var service in services)
        {
            service.Bootstrap();
        }
        
        Console.WriteLine();
        Console.WriteLine("Enabled services:");
        
        foreach (var service in services)
        {
            Console.WriteLine(
                $"- {service.Name}: {string.Join(", ", service.Endpoints.Select(e => $"{e.PortsAndProtocol.Protocol} -> {string.Join(", ", e.PortsAndProtocol.Ports)}"))}");
        }

        if (_options.DefaultSubscription.HasValue)
        {
            Console.WriteLine();
            Console.WriteLine("Creating a default subscription...");

            var subscriptionControlPlane = new SubscriptionControlPlane(_eventPipeline, new SubscriptionResourceProvider(_logger));
            var existingSubscriptionOperation = subscriptionControlPlane.Get(SubscriptionIdentifier.From(_options.DefaultSubscription.Value));
            if (existingSubscriptionOperation.Result == OperationResult.NotFound)
            {
                subscriptionControlPlane.Create(SubscriptionIdentifier.From(_options.DefaultSubscription.Value), "Topaz - Default");
                Console.WriteLine("Default subscription created.");
            }
        }
        
        var httpEndpoints = new List<IEndpointDefinition>();
        var amqpEndpoints = new List<IEndpointDefinition>();

        ExtractEndpointsForProtocols(services, httpEndpoints, [Protocol.Http, Protocol.Https]);
        ExtractEndpointsForProtocols(services, amqpEndpoints, [Protocol.Amqp]);

        await CreateWebserverForHttpEndpointsAsync([.. httpEndpoints], idFactory, cancellationToken);
        CreateAmqpListenersForAmpqEndpoints([.. amqpEndpoints]);
        
        Console.WriteLine();
        Console.WriteLine("Topaz.Host listening to incoming requests...");
        Console.WriteLine();
        
        // Wait for cancellation
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
    }

    private void CreateAmqpListenersForAmpqEndpoints(IEndpointDefinition[] endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            // Create plain AMQP listener (for Azure SDK with UseDevelopmentEmulator=true)
            CreateAmqpListener(endpoint, useTls: false);
            
            // Also create AMQPS listener with TLS (for MassTransit and other clients that require TLS)
            // Use port+1 for TLS version (e.g., 8889 -> 8890 for TLS)
            if ((!string.IsNullOrEmpty(_options.CertificateFile) && !string.IsNullOrEmpty(_options.CertificateKey)) ||
                File.Exists("topaz.crt"))
            {
                CreateAmqpListener(endpoint, useTls: true);
            }
        }
    }
    
    private void CreateAmqpListener(IEndpointDefinition endpoint, bool useTls)
    {
        var scheme = useTls ? "amqps" : "amqp";
        // Use port+1 for TLS (e.g., 8889 for plain, 5671 for TLS)
        var port = useTls ? GlobalSettings.AmqpTlsConnectionPort : endpoint.PortsAndProtocol.Ports[0];
        var address = new Address($"{scheme}://{_topazIpAddress}:{port}");
        var listener = new ContainerHost(address);
        
        // Configure TLS if requested
        if (useTls)
        {
            var certificate = LoadCertificate();
            listener.Listeners[0].SSL.Certificate = certificate;
            listener.Listeners[0].SSL.ClientCertificateRequired = false;
            listener.Listeners[0].SSL.CheckCertificateRevocation = false;
        }
        
        listener.Listeners[0].SASL.EnableMechanism(new Symbol("MSSBCBS"), new TopazSaslProfile(new Symbol("MSSBCBS")));
        listener.Listeners[0].SASL.EnableAnonymousMechanism = true;
        listener.Listeners[0].AMQP.MaxFrameSize = 262144;
        
        listener.RegisterRequestProcessor("$cbs", new CbsProcessor());
        listener.RegisterRequestProcessor("$management", new ManagementProcessor());
        listener.RegisterLinkProcessor(new LinkProcessor(_logger));

        // Frame traces should be enabled only if LogLevel is set to Debug
        if (_logger.LogLevel == LogLevel.Debug)
        {
            Trace.TraceLevel = TraceLevel.Frame;
            Trace.TraceListener = (l, f, a) => _logger.LogDebug(nameof(Host), nameof(CreateAmqpListener), $"[{address.Scheme}://{address.Host}:{address.Port}]: {string.Format(f, a)}");
        }

        Threads.Add(new Thread(() =>
        {
            var listenerAddress = $"{address.Scheme}://{address.Host}:{address.Port}";
            
            try
            {
                listener.Open();
                _logger.LogInformation($"AMQP listener started: {listenerAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open Topaz host listener for AMQP ({listenerAddress}). Error: {ex.Message}.");
            }
        }));
   
        Threads.Last().Start();
    }

    private void ExtractEndpointsForProtocols(IServiceDefinition[] services, List<IEndpointDefinition> httpEndpoints, Protocol[] protocols)
    {
        foreach (var service in services)
        {
            _logger.LogDebug(nameof(Host), nameof(ExtractEndpointsForProtocols), $"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                _logger.LogDebug(nameof(Host), nameof(ExtractEndpointsForProtocols),$"Processing {service.Name} endpoints...");

                if (!protocols.Contains(endpoint.PortsAndProtocol.Protocol)) continue;
                
                _logger.LogDebug(nameof(Host), nameof(ExtractEndpointsForProtocols),$"Processing endpoint of {service.Name} service.");
                httpEndpoints.Add(endpoint);
            }
        }
    }

    private async Task CreateWebserverForHttpEndpointsAsync(IEndpointDefinition[] httpEndpoints, CorrelationIdFactory idFactory, CancellationToken cancellationToken)
    {
        var host = new WebHostBuilder()
            .UseKestrel((_, hostOptions) =>
            {
                var usedPorts = new List<int>();
                foreach (var httpEndpoint in httpEndpoints)
                {
                    switch (httpEndpoint.PortsAndProtocol.Protocol)
                    {
                        case Protocol.Http:
                            foreach (var port in httpEndpoint.PortsAndProtocol.Ports)
                            {
                                if (usedPorts.Contains(port))
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                        "Using port {0} will be skipped as it's already registered.", Guid.Empty, port);
                                    continue;
                                }
                                
                                _logger.LogInformation($"Topaz will listen to HTTP requests on: {_topazIpAddress}:{port}");
                                hostOptions.Listen(IPAddress.Parse(_topazIpAddress), port);
                                
                                usedPorts.Add(port);
                            }
                            
                            break;
                        case Protocol.Https:
                            foreach (var port in httpEndpoint.PortsAndProtocol.Ports)
                            {
                                _logger.LogInformation($"Starting HTTPS endpoint: {_topazIpAddress}:{port}");
                                
                                if (usedPorts.Contains(port))
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),$"Using port {port} will be skipped as it's already registered.");
                                    continue;
                                }

                                if (!IsRunningInsideContainer() && port == GlobalSettings.AdditionalResourceManagerPort)
                                {
                                    _logger.LogWarning("Port 443 used by HTTPS endpoint will be skipped as Topaz isn't running inside a container.");
                                    continue;
                                }
                                
                                if (!IsRunningInsideContainer() && port == GlobalSettings.AmqpTlsConnectionPort)
                                {
                                    _logger.LogWarning($"Port {GlobalSettings.AmqpTlsConnectionPort} used by HTTPS endpoint will be skipped as Topaz isn't running inside a container.");
                                    continue;
                                }
                            
                                hostOptions.Listen(IPAddress.Parse(_topazIpAddress), port, listenOptions =>
                                {
                                    _logger.LogInformation($"Topaz will listen to HTTPS requests on: {_topazIpAddress}:{port}");
                                    
                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                    {
                                        _logger.LogInformation($"Using the provided PFX certificate for listening to requests on [{port}, {httpEndpoint.PortsAndProtocol.Protocol}].");
                                        listenOptions.UseHttps("topaz.pfx", "qwerty");
                                    }
                                    else
                                    {
                                        ConfigurePemCertificate(listenOptions);
                                    }
                                });
                                
                                usedPorts.Add(port);
                            }
                            
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
                        // Generate new correlation ID, which can be fetched by any class via DI
                        idFactory.GenerateNew();
                        
                        await _router.MatchAndExecuteEndpoint(httpEndpoints, context).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        _logger.LogError(ex);

                        await context.Response.WriteAsync(ex.Message, cancellationToken: cancellationToken);
                    }
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);
    }

    private X509Certificate2 LoadCertificate()
    {
        string certPem;
        string keyPem;
                                    
        if (!string.IsNullOrEmpty(_options.CertificateFile) &&
            !string.IsNullOrEmpty(_options.CertificateKey))
        {
            _logger.LogInformation("Loading certificate for AMQP TLS from provided files.");
            certPem = File.ReadAllText(_options.CertificateFile);
            keyPem = File.ReadAllText(_options.CertificateKey);
        }
        else
        {
            certPem = File.ReadAllText("topaz.crt");
            keyPem = File.ReadAllText("topaz.key");
        }
                                    
        return X509Certificate2.CreateFromPem(certPem, keyPem);
    }

    private void ConfigurePemCertificate(ListenOptions listenOptions)
    {
        string? certPem;
        string? keyPem;
                                    
        if (!string.IsNullOrEmpty(_options.CertificateFile) &&
            !string.IsNullOrEmpty(_options.CertificateKey))
        {
            _logger.LogInformation("Using provided certificate file instead of the default one.");
                                        
            certPem = File.ReadAllText(_options.CertificateFile);
            keyPem = File.ReadAllText(_options.CertificateKey);
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
