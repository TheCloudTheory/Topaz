using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Primitives;
using Topaz.CloudEnvironment;
using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.Host.AMQP;
using Topaz.Service.Authorization;
using Topaz.Service.AppService;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.Entra;
using Topaz.Service.EventHub;
using Topaz.Service.Insights;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagementGroup;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage;
using Topaz.Service.Storage.Services;
using Topaz.Service.Subscription;
using Topaz.Service.Disk;
using Topaz.Service.AppConfiguration;
using Topaz.Service.VirtualMachine;
using Topaz.Service.VirtualNetwork;
using Topaz.Service.LoadBalancer;
using Topaz.Service.Sql;
using Topaz.Service.CosmosDb;
using Topaz.FinOps;
using Spectre.Console;
using Topaz.Chaos;
using Topaz.ForwardProxy;
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
    /// IP address used by Topaz to listen to incoming requests. Note that the address is controlled by the
    /// host itself, while ports and protocols are the responsibility of appropriate services.
    /// </summary>
    private readonly string _topazIpAddress;

    private readonly GlobalOptions _options;
    private readonly ITopazLogger _logger;
    private readonly AppServiceForwardProxy _appServiceForwardProxy;

    public Host(GlobalOptions options, ITopazLogger logger)
    {
        _options = options;
        _logger = logger;
        _eventPipeline = new Pipeline(logger);
        _router = new Router(_eventPipeline, options, logger);
        _topazIpAddress = IsRunningInsideContainer() ? "0.0.0.0" :
            string.IsNullOrWhiteSpace(options.EmulatorIpAddress) ? "127.0.0.1" : options.EmulatorIpAddress;
        _appServiceForwardProxy = new AppServiceForwardProxy(new HttpClient(), logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.Title = "--- Topaz.Host ---";

        Console.WriteLine();
        Console.WriteLine("  ████████╗ ██████╗ ██████╗  █████╗ ███████╗");
        Console.WriteLine("     ██╔══╝██╔═══██╗██╔══██╗██╔══██╗╚══███╔╝");
        Console.WriteLine("     ██║   ██║   ██║██████╔╝███████║  ███╔╝ ");
        Console.WriteLine("     ██║   ██║   ██║██╔═══╝ ██╔══██║ ███╔╝  ");
        Console.WriteLine("     ██║   ╚██████╔╝██║     ██║  ██║███████╗");
        Console.WriteLine("     ╚═╝    ╚═════╝ ╚═╝     ╚═╝  ╚═╝╚══════╝");
        Console.WriteLine();
        Console.WriteLine($"  Azure emulator  •  v{ThisAssembly.AssemblyInformationalVersion}");
        Console.WriteLine();

        Bootstrap();

        GlobalDnsEntries.ConfigureLogger(_logger);

        var idFactory = new CorrelationIdFactory();
        var services = new IServiceDefinition[]
        {
            new AzureStorageService(_logger),
            new TableStorageService(_eventPipeline, _logger),
            new QueueStorageService(_eventPipeline, _logger),
            new ResourceGroupService(_eventPipeline, _logger),
            new SubscriptionService(_eventPipeline, _logger),
            new KeyVaultService(_eventPipeline, _logger),
            new EventHubService(_logger),
            new BlobStorageService(_eventPipeline, _logger),
            new TopazCloudEnvironmentService(),
            new ServiceBusService(_eventPipeline, _logger, SessionMessageStore.ClearQueue),
            new ResourceManagerService(_eventPipeline, _logger, cancellationToken),
            new VirtualNetworkService(_eventPipeline, _logger),
            new NetworkSecurityGroupService(_eventPipeline, _logger),
            new NetworkInterfaceService(_eventPipeline, _logger),
            new PublicIpAddressService(_eventPipeline, _logger),
            new VirtualMachineService(_eventPipeline, _logger),
            new DiskService(_eventPipeline, _logger),
            new AppConfigurationService(_eventPipeline, _logger),
            new LoadBalancerService(_eventPipeline, _logger),
            new ManagedIdentityService(_eventPipeline, _logger),
            new ManagementGroupService(_eventPipeline, _logger),
            new ResourceAuthorizationService(),
            new ResourceGroupAuthorizationService(),
            new RoleDefinitionService(_eventPipeline, _logger),
            new RoleAssignmentService(_eventPipeline, _logger),
            new InsightsService(),
            new EntraService(_eventPipeline, _logger),
            new ContainerRegistryService(_eventPipeline, _logger),
            new AppServicePlanService(_eventPipeline, _logger),
            new AppServiceSiteService(_logger),
            new AppServiceKuduService(_logger),
            new SqlService(_eventPipeline, _logger),
            new CosmosDbService(_eventPipeline, _logger),
            new FinOpsService(_logger),
            new ChaosService(_logger),
            new ForwardProxyService()
        };

        _logger.ConfigureIdFactory(idFactory);

        foreach (var service in services)
        {
            service.Register();
        }

        foreach (var service in services)
        {
            service.Initialize();
        }

        PrintServicesTable(services);

        if (_options.DefaultSubscription.HasValue)
        {
            Console.WriteLine();
            Console.WriteLine("Creating a default subscription...");

            var subscriptionControlPlane =
                SubscriptionControlPlane.New(_eventPipeline, _logger);
            var existingSubscriptionOperation =
                subscriptionControlPlane.Get(SubscriptionIdentifier.From(_options.DefaultSubscription.Value));
            if (existingSubscriptionOperation.Result == OperationResult.NotFound)
            {
                subscriptionControlPlane.Create(SubscriptionIdentifier.From(_options.DefaultSubscription.Value),
                    "Topaz - Default", null);
                Console.WriteLine("Default subscription created.");
            }
        }

        var httpEndpoints = new List<IEndpointDefinition>();
        var amqpEndpoints = new List<IEndpointDefinition>();

        ExtractEndpointsForProtocols(services, httpEndpoints, [Protocol.Http, Protocol.Https]);
        ExtractEndpointsForProtocols(services, amqpEndpoints, [Protocol.Amqp]);

        httpEndpoints.Add(new GetHealthEndpoint());

        // Start the built-in HTTP CONNECT proxy. This remaps port-443 CONNECT tunnels
        // targeting Topaz hostnames to the resource-manager port so that MSAL's user-realm
        // discovery pre-flight works on non-Docker local installations without root privileges.
        var proxy = new ConnectProxy(_topazIpAddress, _logger);
        _ = Task.Run(() => proxy.RunAsync(cancellationToken), cancellationToken);

        if (!IsRunningInsideContainer())
        {
            Console.WriteLine();
            Console.WriteLine("  Topaz HTTPS proxy started.");
            Console.WriteLine("  To use ROPC authentication (az login --username --password),");
            Console.WriteLine("  set the following environment variable before running Azure CLI commands:");
            Console.WriteLine();
            Console.WriteLine(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"    set HTTPS_PROXY=http://127.0.0.1:{GlobalSettings.ConnectProxyPort}"
                : $"    export HTTPS_PROXY=http://127.0.0.1:{GlobalSettings.ConnectProxyPort}");
            Console.WriteLine();

            HostState.HttpsConnectProxyAvailable = true;
        }

        await CreateWebserverForHttpEndpointsAsync([.. httpEndpoints], idFactory, cancellationToken);
        CreateAmqpListenersForAmpqEndpoints([.. amqpEndpoints]);

        var backgroundServices = new ITopazBackgroundService[]
        {
            new KeyVaultSoftDeletePurgeScheduler(
                KeyVaultControlPlane.New(_eventPipeline, _logger),
                SubscriptionControlPlane.New(_eventPipeline, _logger),
                _logger,
                GlobalSettings.SoftDeletePurgeSchedulerInterval),
            new KeyVaultSecretsSoftDeletePurgeScheduler(
                KeyVaultControlPlane.New(_eventPipeline, _logger),
                new KeyVaultSecretsDataPlane(_logger, new KeyVaultResourceProvider(_logger)),
                SubscriptionControlPlane.New(_eventPipeline, _logger),
                _logger,
                GlobalSettings.SoftDeletePurgeSchedulerInterval),
            new GeoReplicationSyncScheduler(
                AzureStorageControlPlane.New(_logger),
                SubscriptionControlPlane.New(_eventPipeline, _logger),
                _logger,
                TimeSpan.FromSeconds(30)),
            new ExpiredDocumentsPurgeScheduler(_eventPipeline, TimeSpan.FromSeconds(60), _logger),
            new ServiceBusMessageExpiryScheduler(
                new Service.ServiceBus.Filtering.ServiceBusRuleLoader(GlobalSettings.MainEmulatorDirectory, _logger),
                _logger,
                TimeSpan.FromSeconds(30)),
        };

        InFlightMessageStore.SetRuleLoader(
            new Service.ServiceBus.Filtering.ServiceBusRuleLoader(GlobalSettings.MainEmulatorDirectory, _logger));

        new BackgroundServiceOrchestrator(backgroundServices, _logger).StartAll(cancellationToken);

        if (!AcrDockerExecutor.IsAvailable())
        {
            _logger.LogDebug(nameof(Host), nameof(StartAsync),
                "Docker is not available — ACR DockerBuildRequest runs will use immediate-Succeeded emulation.");
            Console.WriteLine();
            Console.WriteLine("  [warning] Docker not detected. ACR DockerBuildRequest runs will report");
            Console.WriteLine("            immediate Succeeded without real execution.");
        }

        if (ChaosStateProvider.IsEnabled)
        {
            Console.WriteLine();
            AnsiConsole.MarkupLine("  [yellow]WARNING![/] Chaos is enabled. This will cause random failures.");
            Console.WriteLine();
        }

        Console.WriteLine();
        AnsiConsole.MarkupLine("  [green]✓[/] Topaz is ready — listening for requests");
        Console.WriteLine();

        // Wait for cancellation
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
    }

    private void Bootstrap()
    {
        if (Directory.Exists(GlobalSettings.MainEmulatorDirectory))
        {
            _logger.LogDebug(nameof(Host), nameof(Bootstrap), "Emulator directory already exists.");
        }
        else
        {
            Directory.CreateDirectory(GlobalSettings.MainEmulatorDirectory);
            _logger.LogDebug(nameof(Host), nameof(Bootstrap), "Emulator directory created.");
        }

        if (File.Exists(GlobalSettings.GlobalDnsEntriesFilePath))
        {
            _logger.LogDebug(nameof(Host), nameof(Bootstrap), "Global DNS entries file already exists.");
            return;
        }

        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath,
            JsonSerializer.Serialize(new GlobalDnsEntries()));
        _logger.LogDebug(nameof(Host), nameof(Bootstrap), "Global DNS entries file created.");
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

        // Pad Open/Attach frames so Python's positional frame[] access (frame[9], frame[11], frame[13])
        // doesn't raise IndexError. AMQPNetLite 2.5.1 omits trailing null fields; this handler ensures
        // Properties are always included, forcing the serializer to emit all preceding fields too.
        listener.Listeners[0].HandlerFactory = _ => AmqpFramePaddingHandler.Instance;

        listener.RegisterRequestProcessor("$management", new ManagementProcessor(_logger));
        listener.RegisterLinkProcessor(new LinkProcessor(_logger,
            new Service.ServiceBus.Filtering.ServiceBusRuleLoader(GlobalSettings.MainEmulatorDirectory, _logger)));

        InFlightMessageStore.SetRuleLoader(
            new Service.ServiceBus.Filtering.ServiceBusRuleLoader(GlobalSettings.MainEmulatorDirectory, _logger));

        // Frame traces should be enabled only if LogLevel is set to Debug
        if (_logger.LogLevel == LogLevel.Debug)
        {
            Trace.TraceLevel = TraceLevel.Frame;
            Trace.TraceListener = (l, f, a) => _logger.LogDebug(nameof(Host), nameof(CreateAmqpListener),
                $"[{address.Scheme}://{address.Host}:{address.Port}]: {string.Format(f, a)}");
        }

        Threads.Add(new Thread(() =>
        {
            var listenerAddress = $"{address.Scheme}://{address.Host}:{address.Port}";

            try
            {
                listener.Open();
                _logger.LogDebug(nameof(Host), nameof(CreateAmqpListener), $"AMQP listener started: {listenerAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Failed to open Topaz host listener for AMQP ({listenerAddress}). Error: {ex.Message}.");
            }
        }));

        Threads.Last().Start();
    }

    private void ExtractEndpointsForProtocols(IServiceDefinition[] services, List<IEndpointDefinition> httpEndpoints,
        Protocol[] protocols)
    {
        foreach (var service in services)
        {
            _logger.LogDebug(nameof(Host), nameof(ExtractEndpointsForProtocols),
                $"Processing {service.Name} service...");

            foreach (var endpoint in service.Endpoints)
            {
                if (!protocols.Contains(endpoint.PortsAndProtocol.Protocol)) continue;

                _logger.LogDebug(nameof(Host), nameof(ExtractEndpointsForProtocols),
                    $"Processing endpoint [{endpoint.GetType().Name}] of {service.Name} service: {endpoint.PortsAndProtocol.Protocol}:{string.Join(", ", endpoint.PortsAndProtocol.Ports)} -> [{string.Join(" | ", endpoint.Endpoints)}]");
                httpEndpoints.Add(endpoint);
            }
        }
    }

    private async Task CreateWebserverForHttpEndpointsAsync(IEndpointDefinition[] httpEndpoints,
        CorrelationIdFactory idFactory, CancellationToken cancellationToken)
    {
        var host = new WebHostBuilder()
            .UseKestrel((_, hostOptions) =>
            {
                hostOptions.AllowSynchronousIO = true;
                var usedPorts = new List<int>();
                hostOptions.Listen(IPAddress.Parse(_topazIpAddress), ForwardProxySettings.DefaultPort, listenOptions =>
                {
                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync), "Enabling forward proxy.");
                    
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        listenOptions.UseHttps("topaz.pfx", "qwerty");
                    }
                    else
                    {
                        ConfigurePemCertificate(listenOptions);
                    }
                });
                
                usedPorts.Add(ForwardProxySettings.DefaultPort);
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

                                _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                    $"Topaz will listen to HTTP requests on: {_topazIpAddress}:{port}");
                                hostOptions.Listen(IPAddress.Parse(_topazIpAddress), port);

                                usedPorts.Add(port);
                            }

                            break;
                        case Protocol.Https:
                            foreach (var port in httpEndpoint.PortsAndProtocol.Ports)
                            {
                                _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                    $"Starting HTTPS endpoint: {_topazIpAddress}:{port}");

                                if (usedPorts.Contains(port))
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                        $"Using port {port} will be skipped as it's already registered.");
                                    continue;
                                }

                                if (!IsRunningInsideContainer() && port == GlobalSettings.HttpsPort)
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                        "Port 443 used by HTTPS endpoint will be skipped as Topaz isn't running inside a container.");
                                    continue;
                                }

                                if (!IsRunningInsideContainer() && port == GlobalSettings.AmqpTlsConnectionPort)
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                        $"Port {GlobalSettings.AmqpTlsConnectionPort} used by HTTPS endpoint will be skipped as Topaz isn't running inside a container.");
                                    continue;
                                }

                                hostOptions.Listen(IPAddress.Parse(_topazIpAddress), port, listenOptions =>
                                {
                                    _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                        $"Topaz will listen to HTTPS requests on: {_topazIpAddress}:{port}");

                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                    {
                                        _logger.LogDebug(nameof(Host), nameof(CreateWebserverForHttpEndpointsAsync),
                                            $"Using the provided PFX certificate for listening to requests on [{port}, {httpEndpoint.PortsAndProtocol.Protocol}].");
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

                        if (_appServiceForwardProxy.CanForward(context.Request.Host.Host))
                        {
                            var response = await _appServiceForwardProxy.Send(context);
                            
                            context.Response.StatusCode = (int)response.StatusCode;

                            // Copy upstream headers before writing the body — once WriteAsync
                            // is called, the response is flushed and headers can no longer be set.
                            // Skip Transfer-Encoding: HttpClient already decoded chunked bodies
                            // into a plain byte array via ReadAsByteArrayAsync.
                            foreach (var header in response.Headers)
                            {
                                if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                context.Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
                            }

                            if (response.StatusCode != HttpStatusCode.NoContent)
                            {
                                var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                                context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
                                if (!HttpMethods.IsHead(context.Request.Method))
                                {
                                    await context.Response.Body.WriteAsync(responseBytes, cancellationToken);
                                }
                            }
                            
                            return;
                        }

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
            _logger.LogDebug(nameof(Host), nameof(LoadCertificate),
                "Loading certificate for AMQP TLS from provided files.");
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

    private static Table BuildServicesTable(IServiceDefinition[] services, bool topazServices)
    {
        var t = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Service[/]")
            .AddColumn("[bold]Endpoints[/]");

        foreach (var s in services)
        {
            if (s.IsTopazService != topazServices) continue;

            var grouped = s.Endpoints
                .GroupBy(e => e.PortsAndProtocol.Protocol)
                .Select(g =>
                {
                    var ports = g.SelectMany(e => e.PortsAndProtocol.Ports)
                        .Distinct()
                        .OrderBy(p => p);
                    return $"{g.Key}: {string.Join(", ", ports)}";
                });
            t.AddRow(s.Name, string.Join("  |  ", grouped));
        }

        return t;
    }

    private static void PrintServicesTable(IServiceDefinition[] services)
    {
        var hasAzure = false;
        var hasTopaz = false;

        foreach (var s in services)
        {
            if (s.IsTopazService) hasTopaz = true;
            else hasAzure = true;
        }

        AnsiConsole.WriteLine();

        if (hasAzure)
        {
            AnsiConsole.MarkupLine("[bold]Azure Services[/]");
            AnsiConsole.Write(BuildServicesTable(services, topazServices: false));
        }

        if (hasTopaz)
        {
            AnsiConsole.MarkupLine("[bold]Topaz Services[/]");
            AnsiConsole.Write(BuildServicesTable(services, topazServices: true));
        }

        AnsiConsole.WriteLine();
    }

    private void ConfigurePemCertificate(ListenOptions listenOptions)
    {
        string? certPem;
        string? keyPem;

        if (!string.IsNullOrEmpty(_options.CertificateFile) &&
            !string.IsNullOrEmpty(_options.CertificateKey))
        {
            _logger.LogDebug(nameof(Host), nameof(LoadCertificate),
                "Using provided certificate file instead of the default one.");

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