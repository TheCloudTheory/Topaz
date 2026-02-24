using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Topaz.CLI.Commands;
using Topaz.Dns;
using Topaz.Documentation.Command;
using Topaz.Service.Entra;
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
    internal static CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            var registrations = new ServiceCollection();
            RegisterDependencies(registrations);

            var registrar = new TypeRegistrar(registrations);

            var result = await BootstrapCli(args, registrar);
            return result;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            await Console.Error.WriteLineAsync(ex.StackTrace);

            return 1;
        }
    }

    private static void RegisterDependencies(ServiceCollection registrations)
    {
        registrations.AddSingleton<ITopazLogger, PrettyTopazLogger>();
    }

    private static Task<int> BootstrapCli(string[] args, TypeRegistrar registrar)
    {
        CreateEmulatorDirectoryIfNeeded();

        var app = new CommandApp(registrar);
        
        app.Configure(FindAndRegisterCommands);

        return app.RunAsync(args);
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
            typeof(GenericManagedIdentityCommand)
        };
        
        var commands = Assembly.GetExecutingAssembly()
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetExportedTypes()).Where(type => typeof(IEmulatorCommand).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => Activator.CreateInstance(type) as IEmulatorCommand)
            .Where(command => command != null)
            .ToList();

        commands.Add(new GenericStartCommand());

        foreach (var command in commands)
        {
            command!.Configure(config);
        }
    }

    private static void CreateEmulatorDirectoryIfNeeded()
    {
        if (Directory.Exists(GlobalSettings.MainEmulatorDirectory))
        {
            Console.WriteLine("Emulator directory already exists.");
        }
        else
        {
            Directory.CreateDirectory(GlobalSettings.MainEmulatorDirectory);
            Console.WriteLine("Emulator directory created.");
        }
        
        var entra = new EntraService(new PrettyTopazLogger());
        entra.Bootstrap();
        
        if (File.Exists(GlobalSettings.GlobalDnsEntriesFilePath))
        {
            Console.WriteLine("Global DNS entries file already exists.");
            return;
        }
        
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, JsonSerializer.Serialize(new GlobalDnsEntries()));
        Console.WriteLine("Global DNS entries file created.");
    }
}