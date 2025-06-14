using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.ResourceGroup.Commands;

public sealed class DeleteResourceGroupCommand(ITopazLogger logger) : Command<DeleteResourceGroupCommand.DeleteResourceGroupCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, DeleteResourceGroupCommandSettings settings)
    {
        this._topazLogger.LogInformation("Deleting resource group...");

        var rp = new ResourceProvider(this._topazLogger);
        rp.Delete(settings.Name!);

        this._topazLogger.LogInformation("Resource group deleted.");

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
