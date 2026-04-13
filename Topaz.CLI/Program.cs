using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Commands;
using Topaz.Service.ContainerRegistry.Commands;
using Topaz.Service.EventHub.Commands;
using Topaz.Service.KeyVault.Commands;
using Topaz.Service.ManagedIdentity.Commands;
using Topaz.Service.ResourceGroup.Commands;
using Topaz.Service.ResourceManager.Commands;
using Topaz.Service.ServiceBus.Commands;
using Topaz.Service.Storage.Commands;
using Topaz.Service.Subscription.Commands;
using Topaz.Shared;

namespace Topaz.CLI;

[UsedImplicitly]
internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        var hostCheck = await CheckHostAsync();
        if (hostCheck != 0)
            return hostCheck;

        return await RunAsync(args);
    }

    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var registrations = new ServiceCollection();
            RegisterDependencies(registrations);

            var registrar = new TypeRegistrar(registrations);

            var app = new CommandApp(registrar);
            app.Configure(FindAndRegisterCommands);

            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            await Console.Error.WriteLineAsync(ex.StackTrace);

            return 1;
        }
    }

    private static async Task<int> CheckHostAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        try
        {
            var response = await client.GetAsync(
                $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health");
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("workingDirectory", out var wdElement))
                return 0;

            var hostDir = Path.GetFullPath(wdElement.GetString() ?? string.Empty);
            var cliDir = Path.GetFullPath(Environment.CurrentDirectory);

            if (!string.Equals(hostDir, cliDir, StringComparison.OrdinalIgnoreCase))
            {
                await Console.Error.WriteLineAsync(
                    $"Topaz Host is running from a different directory ('{hostDir}'). " +
                    "Run the CLI from the same directory as the Host.");
                return 1;
            }

            return 0;
        }
        catch (HttpRequestException)
        {
            await Console.Error.WriteLineAsync(
                "Topaz Host is not running. Please start it first using `topaz-host`.");
            return 1;
        }
        catch (TaskCanceledException)
        {
            await Console.Error.WriteLineAsync(
                "Topaz Host is not running. Please start it first using `topaz-host`.");
            return 1;
        }
    }

    private static void RegisterDependencies(ServiceCollection registrations)
    {
        registrations.AddSingleton<ITopazLogger, PrettyTopazLogger>();
        registrations.AddSingleton<Pipeline, Pipeline>();
    }

    private static void FindAndRegisterCommands(IConfigurator config)
    {
        Console.WriteLine("Searching and configuring commands...");

        // Even though the types will be loaded via reflection, they must be explicitly
        // used so they can be loaded. The issue here is related to GetReferencedAssemblies(),
        // which gets the assemblies referenced in the assembly, not in the project
        // (those are completely different references conceptually).
        // See e.g. https://github.com/dotnet/runtime/issues/57714 for a reference.
        _ = new[]
        {
            typeof(GenericResourceGroupCommand),
            typeof(GenericEventHubCommand),
            typeof(GenericKeyVaultCommand),
            typeof(GenericResourceManagerCommand),
            typeof(GenericServiceBusCommand),
            typeof(GenericStorageCommand),
            typeof(GenericSubscriptionCommand),
            typeof(GenericManagedIdentityCommand),
            typeof(GenericRoleCommand),
            typeof(GenericContainerRegistryCommand)
        };

        var commands = Assembly.GetExecutingAssembly()
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetExportedTypes()).Where(type => typeof(IEmulatorCommand).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => Activator.CreateInstance(type) as IEmulatorCommand)
            .Where(command => command != null)
            .ToList();

        foreach (var command in commands)
        {
            command!.Configure(config);
        }
    }
}
