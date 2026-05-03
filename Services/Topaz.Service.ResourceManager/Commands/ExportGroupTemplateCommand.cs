using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
[CommandDefinition("deployment group export-template", "deployment", "Exports an ARM template from a resource group.")]
[CommandExample("Export template from a resource group", "topaz deployment group export-template \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
[CommandExample("Export template with parameterization options", "topaz deployment group export-template \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\" \\\n    --options \"IncludeParameterDefaultValue,IncludeComments\"")]
public sealed class ExportGroupTemplateCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ExportGroupTemplateCommand.ExportGroupTemplateCommandSettings>
{
    public override int Execute(CommandContext context, ExportGroupTemplateCommandSettings settings)
    {
        logger.LogDebug(nameof(ExportGroupTemplateCommand), nameof(Execute),
            "Executing {0}.{1}.", nameof(ExportGroupTemplateCommand), nameof(Execute));

        var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(settings.SubscriptionId));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.Name!);

        var rgControlPlane = new ResourceGroupControlPlane(
            new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger),
            logger);

        var rgOperation = rgControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (rgOperation.Result == OperationResult.NotFound || rgOperation.Resource == null)
        {
            Console.Error.WriteLine($"Resource group '{settings.Name}' not found.");
            return 1;
        }

        var provider = new ResourceManagerResourceProvider(logger);
        var controlPlane = new ResourceManagerControlPlane(
            provider,
            new TemplateDeploymentOrchestrator(eventPipeline, provider, new SubscriptionDeploymentResourceProvider(logger), logger),
            logger);

        var request = new ExportTemplateRequest
        {
            Resources = ["*"],
            Options = settings.Options,
        };

        var result = controlPlane.ExportTemplate(subscriptionIdentifier, resourceGroupIdentifier, request);
        AnsiConsole.WriteLine(result.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ExportGroupTemplateCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ExportGroupTemplateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;

        [CommandOptionDefinition("Export options: comma-separated list of IncludeParameterDefaultValue, IncludeComments, SkipResourceNameParameterization, SkipAllParameterization.", required: false)]
        [CommandOption("-o|--options")]
        public string? Options { get; set; }
    }
}
