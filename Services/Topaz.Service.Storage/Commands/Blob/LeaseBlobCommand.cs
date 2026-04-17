using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
public sealed class LeaseBlobCommand(ITopazLogger logger) : Command<LeaseBlobCommand.LeaseBlobCommandSettings>
{
    public override int Execute(CommandContext context, LeaseBlobCommandSettings settings)
    {
        logger.LogInformation($"Leasing blob (action: {settings.Action!})...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup);

        var blobPath = $"/{settings.ContainerName}/{settings.BlobName}";
        var dataPlane = new BlobServiceDataPlane(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

        var result = dataPlane.LeaseBlob(
            subscriptionIdentifier, resourceGroupIdentifier,
            settings.AccountName!,
            blobPath,
            settings.Action!,
            settings.LeaseDuration ?? -1,
            settings.ProposedLeaseId,
            settings.LeaseId,
            settings.LeaseBreakPeriod);

        return result.Result switch
        {
            OperationResult.Created or OperationResult.Success =>
                LogLeaseId(result.Resource!.LeaseId),
            OperationResult.Accepted =>
                LogBreakTime(result.Resource!),
            OperationResult.NotFound =>
                LogError($"Blob '{blobPath}' not found."),
            OperationResult.Conflict =>
                LogError("Lease conflict — check current lease state."),
            OperationResult.PreconditionFailed =>
                LogError("Lease ID mismatch."),
            OperationResult.BadRequest =>
                LogError("Bad request — check required headers (e.g. proposed-lease-id for change)."),
            _ =>
                LogError($"Unexpected result: {result.Result}.")
        };
    }

    public override ValidationResult Validate(CommandContext context, LeaseBlobCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");
        if (string.IsNullOrEmpty(settings.ContainerName))
            return ValidationResult.Error("Container name can't be null.");
        if (string.IsNullOrEmpty(settings.BlobName))
            return ValidationResult.Error("Blob name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.Action))
            return ValidationResult.Error("Lease action can't be null.");

        var valid = new[] { "acquire", "renew", "change", "release", "break" };
        if (!valid.Contains(settings.Action.ToLowerInvariant()))
            return ValidationResult.Error($"Invalid lease action '{settings.Action}'. Must be one of: {string.Join(", ", valid)}.");

        return base.Validate(context, settings);
    }

    private int LogLeaseId(string? leaseId)
    {
        logger.LogInformation($"Lease ID: {leaseId}");
        return 0;
    }

    private int LogBreakTime(Models.ContainerLease lease)
    {
        var remaining = lease.BreakTime.HasValue
            ? (int)Math.Max(0, Math.Ceiling((lease.BreakTime.Value - DateTimeOffset.UtcNow).TotalSeconds))
            : 0;
        logger.LogInformation($"Lease breaking — remaining seconds: {remaining}");
        return 0;
    }

    private int LogError(string message)
    {
        logger.LogError(message);
        return 1;
    }

    [UsedImplicitly]
    public sealed class LeaseBlobCommandSettings : CommandSettings
    {
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOption("--action")] public string? Action { get; set; }
        [CommandOption("--lease-duration")] public int? LeaseDuration { get; set; }
        [CommandOption("--lease-id")] public string? LeaseId { get; set; }
        [CommandOption("--proposed-lease-id")] public string? ProposedLeaseId { get; set; }
        [CommandOption("--lease-break-period")] public int? LeaseBreakPeriod { get; set; }
    }
}
