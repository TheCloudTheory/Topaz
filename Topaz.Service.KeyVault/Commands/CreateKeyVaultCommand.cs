using JetBrains.Annotations;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault.Commands;

[UsedImplicitly]
public class CreateKeyVaultCommand(ITopazLogger logger) : Command<CreateKeyVaultCommand.CreateKeyVaultCommandSettings>
{
    public override int Execute(CommandContext context, CreateKeyVaultCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(CreateKeyVaultCommand)}.{nameof(Execute)}.");

        var controlPlane = new KeyVaultControlPlane(new ResourceProvider(logger));
        var kv = controlPlane.Create(settings.Name!, ResourceGroupIdentifier.From(settings.ResourceGroup!), settings.Location!, SubscriptionIdentifier.From(settings.SubscriptionId!));

        logger.LogInformation(kv.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateKeyVaultCommandSettings settings)
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
    
    [UsedImplicitly]
    public sealed class CreateKeyVaultCommandSettings : CommandSettings
    {
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
