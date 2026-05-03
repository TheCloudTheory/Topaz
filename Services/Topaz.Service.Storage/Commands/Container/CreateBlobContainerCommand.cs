using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container create", "azure-storage/container", "Creates a new blob container in a storage account.")]
[CommandExample("Create a container", "topaz storage container create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"mycontainer\"")]
public sealed class CreateBlobContainerCommand(ITopazLogger logger) : Command<CreateBlobContainerCommand.CreateBlobContainerCommandSettings>
{
    public override int Execute(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        AnsiConsole.WriteLine("Creating blob container...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var rp = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var result = rp.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, settings.Name, settings.AccountName);

        if (result.Result != OperationResult.Created)
            return 1;

        AnsiConsole.WriteLine("Container created.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateBlobContainerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Container name can't be null.");
        }
        
        if(string.IsNullOrEmpty(settings.ResourceGroup))
        {
            return ValidationResult.Error("Storage account resource group can't be null.");
        }

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Storage account subscription ID can't be null.");
        }

        return string.IsNullOrEmpty(settings.AccountName) ? 
            ValidationResult.Error("Storage account name can't be null.") 
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateBlobContainerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-n|--name")] public string Name { get; set; } = null!;
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string AccountName { get; set; } = null!;
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
