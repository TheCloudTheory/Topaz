using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Topaz.CLI.Commands;
using Topaz.Service.EventHub.Commands;
using Topaz.Service.KeyVault.Commands;
using Topaz.Service.ResourceGroup.Commands;
using Topaz.Service.ResourceManager.Commands;
using Topaz.Service.ServiceBus.Commands;
using Topaz.Service.Shared.Command;
using Topaz.Service.Storage.Commands;
using Topaz.Service.Subscription.Commands;
using Topaz.Shared;

namespace Topaz.CLI;

[UsedImplicitly]
internal class Program
{
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
        CreateLocalDirectoryForEmulator();

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

    private static void CreateLocalDirectoryForEmulator()
    {
        const string emulatorPath = ".topaz";
        if(Directory.Exists(emulatorPath)) return;
        
        Console.WriteLine("Creating local directory for emulator...");

        Directory.CreateDirectory(".topaz");
    }
}