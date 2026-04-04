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
[CommandDefinition("acr regenerate-credential", "container-registry", "Regenerates an admin password for an Azure Container Registry.")]
[CommandExample("Regenerate password", "topaz acr regenerate-credential \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --password-name password")]
[CommandExample("Regenerate password2", "topaz acr regenerate-credential \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --password-name password2")]
public sealed class RegenerateContainerRegistryCredentialCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<RegenerateContainerRegistryCredentialCommand.RegenerateCredentialCommandSettings>
{
    public override int Execute(CommandContext context, RegenerateCredentialCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(RegenerateContainerRegistryCredentialCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

        var operation = controlPlane.RegenerateCredential(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            settings.Name!,
            settings.PasswordName!);

        if (operation.Result == OperationResult.NotFound)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        if (operation.Result == OperationResult.Failed)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        var props = operation.Resource!.Properties;
        logger.LogInformation($"Username:  {props.AdminUsername}");
        logger.LogInformation($"Password:  {props.AdminPassword}");
        logger.LogInformation($"Password2: {props.AdminPassword2 ?? props.AdminPassword}");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, RegenerateCredentialCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Registry name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.PasswordName))
            return ValidationResult.Error("Password name can't be null. Use 'password' or 'password2'.");

        if (!string.Equals(settings.PasswordName, "password", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings.PasswordName, "password2", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error("Password name must be 'password' or 'password2'.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RegenerateCredentialCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Password to regenerate: 'password' or 'password2'.")]
        [CommandOption("--password-name")]
        public string? PasswordName { get; set; }
    }
}
