using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Sql.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Sql;

public sealed class SqlService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-sql");
    public static IReadOnlyCollection<string>? Subresources =>
    [
        nameof(Subresource.Databases).ToLowerInvariant(),
        nameof(Subresource.ConnectionPolicies).ToLowerInvariant(),
        nameof(Subresource.VulnerabilityAssessments).ToLowerInvariant()
    ];
    public static string UniqueName => "sql";

    public string Name => "Azure SQL";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateSqlServerEndpoint(_eventPipeline, _logger),
        new GetSqlServerEndpoint(_eventPipeline, _logger),
        new DeleteSqlServerEndpoint(_eventPipeline, _logger),
        new UpdateSqlServerEndpoint(_eventPipeline, _logger),
        new ListSqlServersByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListSqlServersBySubscriptionEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateSqlDatabaseEndpoint(_eventPipeline, _logger),
        new GetSqlDatabaseEndpoint(_eventPipeline, _logger),
        new DeleteSqlDatabaseEndpoint(_eventPipeline, _logger),
        new UpdateSqlDatabaseEndpoint(_eventPipeline, _logger),
        new ListSqlDatabasesByServerEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateSqlServerConnectionPolicyEndpoint(_eventPipeline, _logger),
        new GetSqlServerConnectionPolicyEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateSqlServerVulnerabilityAssessmentEndpoint(_eventPipeline, _logger),
        new GetSqlServerVulnerabilityAssessmentEndpoint(_eventPipeline, _logger),
        new ListRestorableDroppedDatabasesByServerEndpoint(),
        new GetTransparentDataEncryptionEndpoint(),
        new CreateOrUpdateTransparentDataEncryptionEndpoint(),
        new GetDatabaseSecurityAlertPolicyEndpoint(),
        new CreateOrUpdateDatabaseSecurityAlertPolicyEndpoint(),
        new GetDatabaseBackupLongTermRetentionPolicyEndpoint(),
        new CreateOrUpdateDatabaseBackupLongTermRetentionPolicyEndpoint(),
        new GetDatabaseBackupShortTermRetentionPolicyEndpoint(),
        new CreateOrUpdateDatabaseBackupShortTermRetentionPolicyEndpoint()
    ];

    public void Bootstrap() { }
}
