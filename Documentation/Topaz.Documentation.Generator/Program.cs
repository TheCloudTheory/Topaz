using System.Reflection;
using System.Text;
using Spectre.Console.Cli;
using Topaz.CLI.Commands;
using Topaz.Documentation.Command;
using Topaz.Service.EventHub.Commands;
using Topaz.Service.KeyVault.Commands;
using Topaz.Service.ManagedIdentity.Commands;
using Topaz.Service.ResourceGroup.Commands;
using Topaz.Service.ResourceManager.Commands;
using Topaz.Service.ServiceBus.Commands;
using Topaz.Service.Storage.Commands;
using Topaz.Service.Subscription.Commands;

Console.WriteLine($"Topaz.Documentation.Generator {ThisAssembly.AssemblyInformationalVersion}");

_ = new[]
{
    typeof(GenericStartCommand),
    typeof(GenericResourceGroupCommand),
    typeof(GenericEventHubCommand),
    typeof(GenericKeyVaultCommand),
    typeof(GenericResourceManagerCommand),
    typeof(GenericServiceBusCommand),
    typeof(GenericStorageCommand),
    typeof(GenericSubscriptionCommand),
    typeof(GenericManagedIdentityCommand)
};

Console.WriteLine("Looking for commands...");

var commands = Assembly.GetExecutingAssembly()
    .GetReferencedAssemblies()
    .Select(Assembly.Load)
    .SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract &&
                   type.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType &&
                                                             interfaceType.GetGenericTypeDefinition() ==
                                                             typeof(ICommand<>)))
    .ToArray();

Console.WriteLine($"Found {commands.Length} commands.");

var commandDefinitionsWithTypes = commands
    .Where(type => type.GetCustomAttribute<CommandDefinitionAttribute>() != null)
    .Select(type => new
    {
        Type = type,
        Definition = type.GetCustomAttribute<CommandDefinitionAttribute>()!
    })
    .ToArray();

var commandExamplesWithTypes = commands
    .SelectMany(type => type.GetCustomAttributes<CommandExampleAttribute>()
        .Select(example => new { Type = type, Example = example }))
    .ToArray();

Console.WriteLine($"Found {commandDefinitionsWithTypes.Length} definitions.");
Console.WriteLine($"Found {commandExamplesWithTypes.Length} examples.");

var settings = Assembly.GetExecutingAssembly()
    .GetReferencedAssemblies()
    .Select(Assembly.Load)
    .SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract &&
                   typeof(CommandSettings).IsAssignableFrom(type))
    .ToArray();

Console.WriteLine($"Found {settings.Length} command settings.");

var groups = commandDefinitionsWithTypes.GroupBy(item => item.Definition.CommandGroup);

foreach (var group in groups)
{
    var index = 1;
    foreach (var item in group)
    {
        var definition = item.Definition;
        var commandType = item.Type;

        Console.WriteLine($"Processing `{definition.CommandName}` from `{definition.CommandGroup}` command.");

        var sb = new StringBuilder();

        // Sidebar positioning
        sb.AppendLine("---");
        sb.AppendLine($"sidebar_position: {index}");
        sb.AppendLine("---");
        sb.AppendLine("");

        // Definition
        sb.AppendLine($"# {definition.CommandName}");
        sb.AppendLine(definition.Description);
        sb.AppendLine("");

        // Find nested settings class within the command class
        var nestedSettingsType = commandType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(nestedType => typeof(CommandSettings).IsAssignableFrom(nestedType));

        // If no nested class found, try to find by naming convention
        if (nestedSettingsType == null)
        {
            var settingsTypeName = commandType.Name.Replace("Command", "Settings");
            nestedSettingsType = settings.FirstOrDefault(s => s.Name == settingsTypeName);
        }

        // Get properties with CommandOptionDefinition attribute
        var properties = nestedSettingsType?
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttribute<CommandOptionDefinitionAttribute>() != null)
            .ToArray() ?? [];

        if (properties.Length != 0)
        {
            sb.AppendLine($"## Options");
        }

        foreach (var property in properties)
        {
            var option = property.GetCustomAttribute<CommandOptionAttribute>();
            var optionDefinition = property.GetCustomAttribute<CommandOptionDefinitionAttribute>();

            if (option != null && optionDefinition != null)
            {
                var shortNames = string.IsNullOrEmpty(option.ShortNames.FirstOrDefault())
                    ? ""
                    : $"-{string.Join(", -", option.ShortNames)}";
                var longNames = string.IsNullOrEmpty(option.LongNames.FirstOrDefault())
                    ? ""
                    : $"--{string.Join(", --", option.LongNames)}";

                var optionNames = string.Join(", ",
                    new[] { shortNames, longNames }.Where(s => !string.IsNullOrEmpty(s)));

                var required = optionDefinition.Required ? "(Required) " : string.Empty;
                
                sb.AppendLine($"* `{optionNames}` - {required}{optionDefinition.Description}");
            }
        }

        // Examples
        var examples = commandExamplesWithTypes
            .Where(exampleItem => exampleItem.Type == commandType)
            .Select(exampleItem => exampleItem.Example)
            .ToArray();

        if (examples.Length > 0)
        {
            sb.AppendLine("");
            sb.AppendLine("## Examples");

            foreach (var example in examples)
            {
                sb.AppendLine("");
                sb.AppendLine($"### {example.Title}");
                sb.AppendLine("```bash");
                sb.AppendLine($"$ {example.Command}");
                sb.AppendLine("```");
            }
        }

        var doc = sb.ToString();

        Console.WriteLine("The generated example preview:");
        Console.WriteLine(doc);

        Console.WriteLine();
        Console.WriteLine("Saving documentation of the command...");

        var basePath = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TOPAZ_BASE_DOCS_PATH"))
            ? Path.Combine("..", "..", "..", "..", "..", "..")
            : Environment.GetEnvironmentVariable("TOPAZ_BASE_DOCS_PATH")!;
        var directoryTemplate = Path.Combine(basePath, "website", "docs", "cli-reference",
            definition.CommandGroup);
        if (!Directory.Exists(directoryTemplate))
        {
            Directory.CreateDirectory(directoryTemplate);
        }

        var commandNameAsFilename = string.Join("-", definition.CommandName.Split(" "));
        var fileTemplate = Path.Combine(directoryTemplate, commandNameAsFilename + ".md");
        File.WriteAllText(fileTemplate, doc);

        index++;
    }
    
    Console.WriteLine("Documentation generated.");
}