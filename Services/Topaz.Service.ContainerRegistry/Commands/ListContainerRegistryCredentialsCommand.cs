using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr list-credentials", "container-registry", "Lists admin credentials for an Azure Container Registry.")]
[CommandExample("List credentials for a registry", "topaz acr list-credentials \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\"")]
public sealed class ListContainerRegistryCredentialsCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListContainerRegistryCredentialsCommand.ListCredentialsCommandSettings>
{
    public override int Execute(CommandContext context, ListCredentialsCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(ListContainerRegistryCredentialsCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.Name!);
        if (operation.Result == OperationResult.NotFound)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        var props = operation.Resource!.Properties;
        if (!props.AdminUserEnabled)
        {
            logger.LogError($"Admin user is disabled for registry '{settings.Name}'. Enable it first with 'acr update --admin-enabled'.");
            return 1;
        }

        logger.LogInformation($"Username: {props.AdminUsername}");
        logger.LogInformation($"Password:  {props.AdminPassword}");
        logger.LogInformation($"Password2: {props.AdminPassword}");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListCredentialsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListCredentialsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Registry name.")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
