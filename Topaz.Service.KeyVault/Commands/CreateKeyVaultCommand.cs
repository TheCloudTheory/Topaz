using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Topaz.Service.KeyVault.Commands;

public class CreateKeyVaultCommand(ITopazLogger logger) : Command<CreateKeyVaultCommand.CreatekeyVaultCommandSettings>
{
    private readonly ITopazLogger _topazLogger = logger;

    public override int Execute(CommandContext context, CreatekeyVaultCommandSettings settings)
    {
        this._topazLogger.LogInformation($"Executing {nameof(CreateKeyVaultCommand)}.{nameof(Execute)}.");

        var controlPlane = new KeyVaultControlPlane(new ResourceProvider(this._topazLogger));
        var kv = controlPlane.Create(settings.Name!, settings.ResourceGroup!, settings.Location!, settings.SubscriptionId!);

        this._topazLogger.LogInformation(kv.ToString());

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

        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
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

        [CommandOption("-s|--subscriptionId")]
        public string? SubscriptionId { get; set; }
    }
}
