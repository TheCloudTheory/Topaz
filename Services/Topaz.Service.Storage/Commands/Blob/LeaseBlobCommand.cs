using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Commands.Blob;

[UsedImplicitly]
[CommandDefinition("storage blob lease", "azure-storage/blob", "Manages lease operations on a blob (acquire, renew, change, release, break).")]
[CommandExample("Acquire a lease", "topaz storage blob lease \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --container-name \"mycontainer\" \\\n    --name \"file.txt\" \\\n    --action \"acquire\" \\\n    --lease-duration 60")]
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
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("--account-name")] public string? AccountName { get; set; }
        [CommandOptionDefinition("(Required) Container name.", required: true)]
        [CommandOption("-c|--container-name")] public string? ContainerName { get; set; }
        [CommandOptionDefinition("(Required) Blob name.", required: true)]
        [CommandOption("-n|--name")] public string? BlobName { get; set; }
        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string? SubscriptionId { get; set; }
        [CommandOptionDefinition("(Required) Lease action: acquire, renew, change, release, or break.", required: true)]
        [CommandOption("--action")] public string? Action { get; set; }
        [CommandOptionDefinition("Lease duration in seconds (-1 for infinite).")]
        [CommandOption("--lease-duration")] public int? LeaseDuration { get; set; }
        [CommandOptionDefinition("Existing lease ID (required for renew, change, release).")]
        [CommandOption("--lease-id")] public string? LeaseId { get; set; }
        [CommandOptionDefinition("Proposed lease ID (required for change action).")]
        [CommandOption("--proposed-lease-id")] public string? ProposedLeaseId { get; set; }
        [CommandOptionDefinition("Break period in seconds (used with break action).")]
        [CommandOption("--lease-break-period")] public int? LeaseBreakPeriod { get; set; }
    }
}
