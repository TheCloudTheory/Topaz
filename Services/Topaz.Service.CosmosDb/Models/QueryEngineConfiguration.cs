namespace Topaz.Service.CosmosDb.Models;

/// <summary>
/// Cosmos DB query engine configuration returned by the account properties endpoint.
/// The SDK's QueryPartitionProvider reads this field during initialization; without it
/// the internal lock is never set and the finalizer crashes with ArgumentNullException.
/// </summary>
public sealed class QueryEngineConfiguration
{
    public int MaxSqlQueryInputLength { get; init; } = 262144;

    public int MaxJoinsPerSqlQuery { get; init; } = 5;

    public int MaxLogicalAndPerSqlQuery { get; init; } = 2000;

    public int MaxLogicalOrPerSqlQuery { get; init; } = 2000;

    public int MaxUdfRefPerSqlQuery { get; init; } = 2;

    public int MaxInExpressionItemsCount { get; init; } = 16000;

    public int QueryMaxGroupByTableCellCount { get; init; } = 500;

    public int QueryMaxGroupByTableSizeKb { get; init; } = 500;

    public bool SqlAllowNonFiniteNumbers { get; init; }

    public bool SqlAllowAggregateFunctions { get; init; } = true;

    public bool SqlAllowSubQuery { get; init; } = true;

    public bool SqlAllowScalarSubQuery { get; init; } = true;

    public bool AllowNewKeywords { get; init; } = true;

    public bool SqlAllowLike { get; init; }

    public bool SqlAllowGroupByClause { get; init; } = true;

    public int MaxSpatialQueryCells { get; init; } = 12;

    public int SpatialMaxGeometryPointCount { get; init; } = 4096;

    public int SqlDisableOptimizationFlags { get; init; }

    public bool SqlAllowTop { get; init; } = true;

    public bool EnableSpatialIndexing { get; init; } = true;
}
