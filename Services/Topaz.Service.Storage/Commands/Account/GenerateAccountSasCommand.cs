using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account generate-sas", "azure-storage/account", "Generates a Shared Access Signature (SAS) token for a storage account.")]
[CommandExample("Generate SAS token", "topaz storage account generate-sas \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --services \"b\" \\\n    --resource-types \"sco\" \\\n    --permissions \"rwdlacup\" \\\n    --expiry \"2030-01-01T00:00:00Z\"")]
public sealed class GenerateAccountSasCommand(ITopazLogger logger)
    : Command<GenerateAccountSasCommand.GenerateAccountSasCommandSettings>
{
    public override int Execute(CommandContext context, GenerateAccountSasCommandSettings settings)
    {
        AnsiConsole.WriteLine("Generating account SAS token...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);

        var request = new ListAccountSasRequest
        {
            SignedServices = settings.Services,
            SignedResourceTypes = settings.ResourceTypes,
            SignedPermission = settings.Permissions,
            SignedExpiry = settings.Expiry,
            SignedStart = settings.Start,
            SignedProtocol = settings.Protocol,
            SignedIp = settings.Ip,
            KeyToSign = settings.KeyToSign
        };

        var result = controlPlane.ListAccountSas(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!, request);

        if (result.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"Storage account '{settings.AccountName}' not found.");
            return 1;
        }

        if (result.Result == OperationResult.Failed || result.Resource == null)
        {
            Console.Error.WriteLine("There was an error generating the account SAS token.");
            return 1;
        }

        AnsiConsole.WriteLine(result.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GenerateAccountSasCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.Services))
            return ValidationResult.Error("--services is required.");

        if (string.IsNullOrEmpty(settings.ResourceTypes))
            return ValidationResult.Error("--resource-types is required.");

        if (string.IsNullOrEmpty(settings.Permissions))
            return ValidationResult.Error("--permissions is required.");

        if (string.IsNullOrEmpty(settings.Expiry))
            return ValidationResult.Error("--expiry is required.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GenerateAccountSasCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) The storage services to sign (b=blob, f=file, q=queue, t=table). Combine letters as needed.", required: true)]
        [CommandOption("--services")] public string? Services { get; set; }

        [CommandOptionDefinition("(Required) The resource types accessible with the SAS (s=service, c=container, o=object).", required: true)]
        [CommandOption("--resource-types")] public string? ResourceTypes { get; set; }

        [CommandOptionDefinition("(Required) The permissions granted by the SAS (e.g. rwdlacup).", required: true)]
        [CommandOption("--permissions")] public string? Permissions { get; set; }

        [CommandOptionDefinition("(Required) Expiry date/time in UTC (ISO 8601, e.g. 2030-01-01T00:00:00Z).", required: true)]
        [CommandOption("--expiry")] public string? Expiry { get; set; }

        [CommandOptionDefinition("Start date/time in UTC (ISO 8601).")]
        [CommandOption("--start")] public string? Start { get; set; }

        [CommandOptionDefinition("Restrict to HTTPS connections only.")]
        [CommandOption("--https-only")] public bool? HttpsOnly { get; set; }

        [CommandOptionDefinition("Restrict to a specific IP address or range.")]
        [CommandOption("--ip")] public string? Ip { get; set; }

        [CommandOptionDefinition("The storage account key to use for signing (key1 or key2).")]
        [CommandOption("--key-to-sign")] public string? KeyToSign { get; set; }

        public string? Protocol => HttpsOnly == true ? "https" : null;
    }
}
