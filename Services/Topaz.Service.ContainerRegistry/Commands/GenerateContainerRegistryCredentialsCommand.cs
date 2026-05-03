using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr generate-credentials", "container-registry", "Generates credentials for an Azure Container Registry token.")]
[CommandExample("Generate credentials for a token", "topaz acr generate-credentials \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\" \\\n    --name \"myregistry\" \\\n    --token-id \"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/my-rg/providers/Microsoft.ContainerRegistry/registries/myregistry/tokens/myToken\"")]
public class GenerateContainerRegistryCredentialsCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<GenerateContainerRegistryCredentialsCommand.GenerateCredentialsCommandSettings>
{
    public override int Execute(CommandContext context, GenerateCredentialsCommandSettings settings)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);

        var controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
        var operation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, settings.RegistryName!);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        var tokenName = ExtractTokenName(settings.TokenId) ?? settings.RegistryName!;
        var expiry = settings.Expiry ?? DateTimeOffset.UtcNow.AddYears(1);

        var passwords = BuildPasswords(settings.PasswordName, expiry);

        AnsiConsole.MarkupLine($"[bold]Username:[/] {tokenName}");
        AnsiConsole.MarkupLine("[bold]Passwords:[/]");
        foreach (var pw in passwords)
        {
            AnsiConsole.MarkupLine($"  [green]{pw.Name}[/]: {pw.Value} (expires: {pw.Expiry:o})");
        }

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

    private static string? ExtractTokenName(string? tokenId)
    {
        if (string.IsNullOrEmpty(tokenId))
            return null;

        var lastSlash = tokenId.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < tokenId.Length - 1)
            return tokenId[(lastSlash + 1)..];

        return null;
    }

    private static (string Name, string Value, DateTimeOffset Expiry)[] BuildPasswords(string? name, DateTimeOffset expiry)
    {
        if (string.Equals(name, "password1", StringComparison.OrdinalIgnoreCase))
            return [("password1", Guid.NewGuid().ToString("N"), expiry)];

        if (string.Equals(name, "password2", StringComparison.OrdinalIgnoreCase))
            return [("password2", Guid.NewGuid().ToString("N"), expiry)];

        return
        [
            ("password1", Guid.NewGuid().ToString("N"), expiry),
            ("password2", Guid.NewGuid().ToString("N"), expiry)
        ];
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
