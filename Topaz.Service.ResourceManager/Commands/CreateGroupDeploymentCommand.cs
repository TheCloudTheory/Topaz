using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
public class CreateGroupDeploymentCommand(ITopazLogger logger) : Command<CreateGroupDeploymentCommand.CreateGroupDeploymentCommandSettings>
{
    private static readonly Stream? EmptyTemplate =
        Assembly.GetExecutingAssembly()?.GetManifestResourceStream("empty-template.json");
    
    public override int Execute(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateGroupDeploymentCommand)}.{nameof(Execute)}.");

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var resourceGroupControlPlane =
            new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), logger);
        var resourceGroup = resourceGroupControlPlane.Get(resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound || resourceGroup.resource == null)
        {
            logger.LogError($"ResourceGroup {resourceGroupIdentifier} not found.");
            return 1;
        }
        
        var controlPlane = new ResourceManagerControlPlane(new ResourceManagerResourceProvider(logger));
        var fakeRequest = new CreateDeploymentRequest
        {
            Properties = new CreateDeploymentRequest.DeploymentProperties
            {
                Mode = settings.Mode.ToString(),
                Template = GetEmptyTemplate(),
            }
        };

        var deploymentName = string.IsNullOrWhiteSpace(settings.Name) ? "empty-template" : settings.Name;
        var deployment = controlPlane.CreateOrUpdateDeployment(resourceGroup.resource.GetSubscription(),
            resourceGroupIdentifier, deploymentName, JsonSerializer.Serialize(fakeRequest, GlobalSettings.JsonOptions),
            resourceGroup.resource.Location, settings.Mode.ToString());

        logger.LogInformation(deployment.resource.ToString());

        return 0;
    }

    private static string? GetEmptyTemplate()
    {
        if (EmptyTemplate is null) return null;

        using var reader = new StreamReader(EmptyTemplate);
        return reader.ReadToEnd();
    }

    public override ValidationResult Validate(CommandContext context, CreateGroupDeploymentCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.ResourceGroup) ? ValidationResult.Error("Resource group name is required when creating a group deployment.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CreateGroupDeploymentCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;
    }

    public enum DeploymentMode
    {
        Incremental,
        Complete
    }
}