using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr generate-credentials", "container-registry", "Generates credentials for an Azure Container Registry token.")]
[CommandExample("Generate credentials for a token", "topaz acr generate-credentials \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --token-id \"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/my-rg/providers/Microsoft.ContainerRegistry/registries/myregistry/tokens/myToken\"")]
public class GenerateContainerRegistryCredentialsCommand(HttpClient httpClient)
    : TopazHttpCommand<GenerateContainerRegistryCredentialsCommand.GenerateCredentialsCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, GenerateCredentialsCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ContainerRegistry/registries/{settings.RegistryName}/generateCredentials";
        var expiry = settings.Expiry ?? DateTimeOffset.UtcNow.AddYears(1);
        var (success, body) = await PostAsync(url, new
        {
            tokenId = settings.TokenId,
            expiry = expiry,
            name = settings.PasswordName
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GenerateCredentialsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");

        if (string.IsNullOrEmpty(settings.RegistryName))
            return ValidationResult.Error("Registry name can't be null.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GenerateCredentialsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Registry name.")]
        [CommandOption("-n|--name")]
        public string? RegistryName { get; set; }

        [CommandOptionDefinition("Resource ID of the token for which credentials are generated.")]
        [CommandOption("--token-id")]
        public string? TokenId { get; set; }

        [CommandOptionDefinition("Expiry date-time for the generated credentials (ISO 8601). Defaults to 1 year from now.")]
        [CommandOption("--expiry")]
        public DateTimeOffset? Expiry { get; set; }

        [CommandOptionDefinition("Specific password to regenerate: password1 or password2. Omit to regenerate both.")]
        [CommandOption("--password-name")]
        public string? PasswordName { get; set; }
    }
}
