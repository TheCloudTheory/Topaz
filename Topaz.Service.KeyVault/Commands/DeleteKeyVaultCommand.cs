using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.KeyVault.Commands;

public sealed class DeleteKeyVaultCommand(ITopazLogger logger) : Command<DeleteKeyVaultCommand.DeleteKeyVaultCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        this._topazLogger.LogInformation("Deleting Azure Key Vault...");

        var rp = new ResourceProvider(this._topazLogger);
        rp.Delete(settings.Name!);

        this._topazLogger.LogInformation("Azure Key Vault deleted.");

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

    public sealed class DeleteKeyVaultCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }
    }
}
