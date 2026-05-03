using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group exists", "group", "Checks whether a resource group exists.")]
[CommandExample("Check if a resource group exists", "topaz group exists \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CheckExistenceResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<CheckExistenceResourceGroupCommand.CheckExistenceResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, CheckExistenceResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(CheckExistenceResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(CheckExistenceResourceGroupCommand), nameof(Execute));

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
        var operation = controlPlane.Get(new SubscriptionIdentifier(Guid.Parse(settings.SubscriptionId)), new ResourceGroupIdentifier(settings.Name!));

        var exists = operation.Result != OperationResult.NotFound;
        AnsiConsole.WriteLine(exists.ToString().ToLowerInvariant());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckExistenceResourceGroupCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CheckExistenceResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;
    }
}
