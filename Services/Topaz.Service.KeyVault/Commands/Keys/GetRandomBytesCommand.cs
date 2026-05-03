using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key random-bytes", "key-vault", "Generates random bytes using the Key Vault random number generator.")]
[CommandExample("Generate 32 random bytes",
    "topaz keyvault key random-bytes --count 32")]
public class GetRandomBytesCommand(ITopazLogger logger) : Command<GetRandomBytesCommand.GetRandomBytesCommandSettings>
{
    public override int Execute(CommandContext context, GetRandomBytesCommandSettings settings)
    {
        var dataPlane = new KeyVaultKeysDataPlane(logger, new KeyVaultResourceProvider(logger));

        var operation = dataPlane.GetRandomBytes(settings.Count);

        if (operation.Result == OperationResult.Failed)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        AnsiConsole.WriteLine(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetRandomBytesCommandSettings settings)
    {
        if (settings.Count < 1 || settings.Count > 128)
            return ValidationResult.Error("Count must be between 1 and 128.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetRandomBytesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Number of random bytes to generate (1–128).", required: true)]
        [CommandOption("-c|--count")]
        public int Count { get; set; }
    }
}
