using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
public sealed class DeleteResourceGroupCommand(ITopazLogger logger) : Command<DeleteResourceGroupCommand.DeleteResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        logger.LogDebug($"Executing {nameof(DeleteResourceGroupCommand)}.{nameof(Execute)}.");
        logger.LogInformation("Deleting resource group...");

        var controlPlane = new ResourceGroupControlPlane(new ResourceProvider(logger), logger);
        var existingResource = controlPlane.Get(settings.Name!);
        if (existingResource.result == OperationResult.NotFound)
        {
            logger.LogError($"Resource group '{settings.Name}' could not be found.");
            return 1;
        }
        
        _= controlPlane.Delete(settings.Name!);
        
        logger.LogInformation("Resource group deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
