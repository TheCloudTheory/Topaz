using Azure.Local.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Azure.Local.Service.ResourceGroup.Commands;

public sealed class DeleteResourceGroupCommand(ILogger logger) : Command<DeleteResourceGroupCommand.DeleteResourceGroupCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        this.logger.LogInformation("Deleting resource group...");

        var rp = new ResourceProvider(this.logger);
        rp.Delete(settings.Name!);

        this.logger.LogInformation("Resource group deleted.");

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

    public sealed class DeleteResourceGroupCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
