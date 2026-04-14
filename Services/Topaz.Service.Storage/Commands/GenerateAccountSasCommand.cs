using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
public sealed class GenerateAccountSasCommand(ITopazLogger logger)
    : Command<GenerateAccountSasCommand.GenerateAccountSasCommandSettings>
{
    public override int Execute(CommandContext context, GenerateAccountSasCommandSettings settings)
    {
        logger.LogInformation("Generating account SAS token...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);
        var controlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);

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
            logger.LogError($"Storage account '{settings.AccountName}' not found.");
            return 1;
        }

        if (result.Result == OperationResult.Failed || result.Resource == null)
        {
            logger.LogError("There was an error generating the account SAS token.");
            return 1;
        }

        logger.LogInformation(result.Resource.ToString());

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
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOption("--services")] public string? Services { get; set; }

        [CommandOption("--resource-types")] public string? ResourceTypes { get; set; }

        [CommandOption("--permissions")] public string? Permissions { get; set; }

        [CommandOption("--expiry")] public string? Expiry { get; set; }

        [CommandOption("--start")] public string? Start { get; set; }

        [CommandOption("--https-only")] public bool? HttpsOnly { get; set; }

        [CommandOption("--ip")] public string? Ip { get; set; }

        [CommandOption("--key-to-sign")] public string? KeyToSign { get; set; }

        public string? Protocol => HttpsOnly == true ? "https" : null;
    }
}
