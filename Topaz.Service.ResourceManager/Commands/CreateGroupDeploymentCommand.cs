using System.Reflection;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
[CommandDefinition("deployment group create", "deployment", "Creates a new deployment on a resource group level")]
public class CreateGroupDeploymentCommand(ITopazLogger logger) : Command<CreateGroupDeploymentCommand.CreateGroupDeploymentCommandSettings>
{
    public override int Execute(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateGroupDeploymentCommand)}.{nameof(Execute)}.");

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroupOperation = resourceGroupControlPlane.Get(SubscriptionIdentifier.From(settings.SubscriptionId), resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound || resourceGroupOperation.Resource == null)
        {
            logger.LogError(resourceGroupOperation.ToString());
            return 1;
        }
        
        var controlPlane = new ResourceManagerControlPlane(new ResourceManagerResourceProvider(logger));
        var fakeRequest = GetTemplate(settings.TemplateFile);

        var deploymentName = DetermineDeploymentName(settings);
        var deployment = controlPlane.CreateOrUpdateDeployment(resourceGroupOperation.Resource.GetSubscription(),
            resourceGroupIdentifier, deploymentName, fakeRequest,
            resourceGroupOperation.Resource.Location, settings.Mode.ToString());

        logger.LogInformation(deployment.resource.ToString());

        return 0;
    }

    private static string DetermineDeploymentName(CreateGroupDeploymentCommandSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Name) ? string.IsNullOrWhiteSpace(settings.TemplateFile) ? 
            "empty-template"
            : GenerateDeploymentNameFromFilename(settings.TemplateFile) 
                : settings.Name;
    }

    private static string GenerateDeploymentNameFromFilename(string templateFile)
    {
        var fi = new FileInfo(templateFile);
        return fi.Name.Replace(fi.Extension, string.Empty);
    }

    private static string GetTemplate(string? templateFile)
    {
        Stream template;
        if (string.IsNullOrWhiteSpace(templateFile))
        {
            var resource = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Topaz.Service.ResourceManager.empty-template.json");
            template = resource ?? throw new InvalidOperationException();
        }
        else
        {
            template = File.OpenRead(templateFile);
        }

        using var reader = new StreamReader(template);
        return reader.ReadToEnd();
    }

    public override ValidationResult Validate(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        if (string.IsNullOrWhiteSpace(settings.TemplateFile))
            return string.IsNullOrEmpty(settings.ResourceGroup)
                ? ValidationResult.Error("Resource group name is required when creating a group deployment.")
                : base.Validate(context, settings);
        
        if (!File.Exists(settings.TemplateFile))
        {
            return ValidationResult.Error("Deployment template file does not exist.");
        }

        return string.IsNullOrEmpty(settings.ResourceGroup) ? ValidationResult.Error("Resource group name is required when creating a group deployment.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateGroupDeploymentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("Subscription ID for the deployment", true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;
        
        [CommandOptionDefinition("Name of the deployment")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("Resource group for the deployment", true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
        
        [CommandOptionDefinition("Deployment mode. Available options are: Incremental, Complete")]
        [CommandOption("--mode")]
        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;
        
        [CommandOptionDefinition("The path to the template file or Bicep file.")]
        [CommandOption("-f|--template-file")]
        public string? TemplateFile { get; set; }
    }

    public enum DeploymentMode
    {
        Incremental,
        Complete
    }
}