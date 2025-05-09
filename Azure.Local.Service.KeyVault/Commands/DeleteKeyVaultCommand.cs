using Azure.Local.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Azure.Local.Service.KeyVault.Commands;

public sealed class DeleteKeyVaultCommand(ILogger logger) : Command<DeleteKeyVaultCommand.DeleteKeyVaultCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, DeleteKeyVaultCommandSettings settings)
    {
        this.logger.LogInformation("Deleting Azure Key Vault...");

        var rp = new ResourceProvider(this.logger);
        rp.Delete(settings.Name!);

        this.logger.LogInformation("Azure Key Vault deleted.");

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
