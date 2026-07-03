using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account generate-service-sas", "azure-storage/account", "Generates a service-level Shared Access Signature (SAS) token for a storage account resource.")]
[CommandExample("Generate service SAS token", "topaz storage account generate-service-sas \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --canonicalized-resource \"/blob/salocal/mycontainer\" \\\n    --resource \"c\" \\\n    --permissions \"rwdl\" \\\n    --expiry \"2030-01-01T00:00:00Z\"")]
public sealed class GenerateServiceSasCommand(HttpClient httpClient)
    : TopazHttpCommand<GenerateServiceSasCommand.GenerateServiceSasCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GenerateServiceSasCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine("Generating service SAS token...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.AccountName}/ListServiceSas";
        var (success, body) = await PostAsync(url, new
        {
            canonicalizedResource = settings.CanonicalizedResource,
            signedResource = settings.Resource,
            signedPermission = settings.Permissions,
            signedExpiry = settings.Expiry,
            signedStart = settings.Start,
            signedProtocol = settings.Protocol,
            signedIp = settings.Ip,
            signedIdentifier = settings.Identifier,
            keyToSign = settings.KeyToSign
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, GenerateServiceSasCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.CanonicalizedResource))
            return ValidationResult.Error("--canonicalized-resource is required.");

        if (string.IsNullOrEmpty(settings.Resource))
            return ValidationResult.Error("--resource is required (e.g. b, c, f, s).");

        if (string.IsNullOrEmpty(settings.Permissions))
            return ValidationResult.Error("--permissions is required.");

        if (string.IsNullOrEmpty(settings.Expiry))
            return ValidationResult.Error("--expiry is required.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GenerateServiceSasCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Canonicalized path of the resource (e.g. /blob/accountname/containername).", required: true)]
        [CommandOption("--canonicalized-resource")] public string? CanonicalizedResource { get; set; }

        [CommandOptionDefinition("(Required) Signed resource type (b=blob, c=container, f=file, s=share).", required: true)]
        [CommandOption("--resource")] public string? Resource { get; set; }

        [CommandOptionDefinition("(Required) The permissions granted by the SAS (e.g. rwdl).", required: true)]
        [CommandOption("--permissions")] public string? Permissions { get; set; }

        [CommandOptionDefinition("(Required) Expiry date/time in UTC (ISO 8601, e.g. 2030-01-01T00:00:00Z).", required: true)]
        [CommandOption("--expiry")] public string? Expiry { get; set; }

        [CommandOptionDefinition("Start date/time in UTC (ISO 8601).")]
        [CommandOption("--start")] public string? Start { get; set; }

        [CommandOptionDefinition("Restrict to HTTPS connections only.")]
        [CommandOption("--https-only")] public bool? HttpsOnly { get; set; }

        [CommandOptionDefinition("Restrict to a specific IP address or range.")]
        [CommandOption("--ip")] public string? Ip { get; set; }

        [CommandOptionDefinition("Stored access policy identifier.")]
        [CommandOption("--identifier")] public string? Identifier { get; set; }

        [CommandOptionDefinition("The storage account key to use for signing (key1 or key2).")]
        [CommandOption("--key-to-sign")] public string? KeyToSign { get; set; }

        public string? Protocol => HttpsOnly == true ? "https" : null;
    }
}
