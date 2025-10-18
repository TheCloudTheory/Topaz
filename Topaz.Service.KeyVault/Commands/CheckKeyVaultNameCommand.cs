using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
public class CheckKeyVaultNameCommand(ITopazLogger logger) : Command<CheckKeyVaultNameCommand.CheckKeyVaultNameCommandSettings>
{
    public override int Execute(CommandContext context, CheckKeyVaultNameCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CheckKeyVaultNameCommand)}.{nameof(Execute)}.");

        var controlPlane = new KeyVaultControlPlane(new ResourceProvider(logger));
        var kv = controlPlane.CheckName(settings.Name!, settings.ResourceType);

        logger.LogInformation(kv.response.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CheckKeyVaultNameCommandSettings settings)
    {
        return string.IsNullOrEmpty(settings.Name) ? ValidationResult.Error("Key vault name can't be null.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class CheckKeyVaultNameCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("--resource-type")]
        public string? ResourceType { get; set; }
    }
}
