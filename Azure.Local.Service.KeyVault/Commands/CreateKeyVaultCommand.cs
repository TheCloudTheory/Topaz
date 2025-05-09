using Azure.Local.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Azure.Local.Service.KeyVault.Commands;

public class CreateKeyVaultCommand(ILogger logger) : Command<CreateKeyVaultCommand.CreatekeyVaultCommandSettings>
{
    private readonly ILogger logger = logger;

    public override int Execute(CommandContext context, CreatekeyVaultCommandSettings settings)
    {
        this.logger.LogInformation($"Executing {nameof(CreateKeyVaultCommand)}.{nameof(Execute)}.");

        var rp = new ResourceProvider(this.logger);
        var kv = rp.Create(settings.Name!, settings.ResourceGroup!, settings.Location!);

        this.logger.LogInformation(kv.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreatekeyVaultCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Resource group name can't be null.");
        }

        if(string.IsNullOrEmpty(settings.Location))
        {
            return ValidationResult.Error("Resource group location can't be null.");
        }

        return base.Validate(context, settings);
    }
    
    public sealed class CreatekeyVaultCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }
    }
}
