using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage container delete", "azure-storage/container", "Deletes a blob container from a storage account.")]
[CommandExample("Delete a container", "topaz storage container delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --name \"mycontainer\"")]
public sealed class DeleteBlobContainerCommand(ITopazLogger logger)
    : Command<DeleteBlobContainerCommand.DeleteBlobContainerCommandSettings>
{
    public override int Execute(CommandContext context, DeleteBlobContainerCommandSettings settings)
    {
        AnsiConsole.WriteLine("Deleting blob container...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new BlobServiceControlPlane(new BlobResourceProvider(logger));
        var result = controlPlane.DeleteContainer(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!,
            settings.Name!);

        if (result.Result != OperationResult.Success)
            return 1;

        AnsiConsole.WriteLine($"Container '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteBlobContainerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteBlobContainerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
    }
}
