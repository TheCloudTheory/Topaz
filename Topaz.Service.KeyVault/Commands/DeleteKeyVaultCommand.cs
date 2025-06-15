using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
public sealed class DeleteKeyVaultCommand(ITopazLogger logger) : Command<DeleteKeyVaultCommand.DeleteKeyVaultCommandSettings>
{
    public override int Execute(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        logger.LogInformation("Deleting Azure Key Vault...");

        var rp = new ResourceProvider(logger);
        rp.Delete(settings.Name!);

        logger.LogInformation("Azure Key Vault deleted.");

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Azure Key Vault name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteKeyVaultCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
