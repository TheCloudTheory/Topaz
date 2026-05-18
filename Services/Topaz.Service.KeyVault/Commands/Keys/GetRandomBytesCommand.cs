using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key random-bytes", "key-vault", "Generates random bytes using the Key Vault random number generator.")]
[CommandExample("Generate 32 random bytes",
    "topaz keyvault key random-bytes --count 32")]
public class GetRandomBytesCommand(HttpClient httpClient) : TopazHttpCommand<GetRandomBytesCommand.GetRandomBytesCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, GetRandomBytesCommandSettings settings)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/rng?api-version=7.4";
        var (success, body) = await PostAsync(url, new { count = settings.Count });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetRandomBytesCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (settings.Count < 1 || settings.Count > 128)
            return ValidationResult.Error("Count must be between 1 and 128.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetRandomBytesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Number of random bytes to generate (1–128).", required: true)]
        [CommandOption("-c|--count")]
        public int Count { get; set; }
    }
}
