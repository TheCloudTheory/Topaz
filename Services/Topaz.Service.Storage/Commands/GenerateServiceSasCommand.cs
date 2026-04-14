using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class GenerateServiceSasCommand(ITopazLogger logger)
    : Command<GenerateServiceSasCommand.GenerateServiceSasCommandSettings>
{
    public override int Execute(CommandContext context, GenerateServiceSasCommandSettings settings)
    {
        logger.LogInformation("Generating service SAS token...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);

        var request = new ListServiceSasRequest
        {
            CanonicalizedResource = settings.CanonicalizedResource,
            SignedResource = settings.Resource,
            SignedPermission = settings.Permissions,
            SignedExpiry = settings.Expiry,
            SignedStart = settings.Start,
            SignedProtocol = settings.Protocol,
            SignedIp = settings.Ip,
            SignedIdentifier = settings.Identifier,
            KeyToSign = settings.KeyToSign
        };

        var result = controlPlane.ListServiceSas(subscriptionIdentifier, resourceGroupIdentifier, settings.AccountName!, request);

        if (result.Result == OperationResult.NotFound)
        {
            logger.LogError($"Storage account '{settings.AccountName}' not found.");
            return 1;
        }

        if (result.Result == OperationResult.Failed || result.Resource == null)
        {
            logger.LogError("There was an error generating the service SAS token.");
            return 1;
        }

        logger.LogInformation(result.Resource.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GenerateServiceSasCommandSettings settings)
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
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOption("--canonicalized-resource")] public string? CanonicalizedResource { get; set; }

        [CommandOption("--resource")] public string? Resource { get; set; }

        [CommandOption("--permissions")] public string? Permissions { get; set; }

        [CommandOption("--expiry")] public string? Expiry { get; set; }

        [CommandOption("--start")] public string? Start { get; set; }

        [CommandOption("--https-only")] public bool? HttpsOnly { get; set; }

        [CommandOption("--ip")] public string? Ip { get; set; }

        [CommandOption("--identifier")] public string? Identifier { get; set; }

        [CommandOption("--key-to-sign")] public string? KeyToSign { get; set; }

        public string? Protocol => HttpsOnly == true ? "https" : null;
    }
}
