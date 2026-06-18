using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class DatabaseAccountResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<CosmosDbService>(logger)
{
    private const string SqlDatabasesSubresource = "sqldatabases";
    private const string SqlContainersSubresource = "sqlcontainers";
    private const string DocsSubdirectory = "docs";

    /// <summary>
    /// Returns the directory path where documents for the given container are stored:
    /// <c>{baseEmulatorPath}/{account}/sqldatabases/{db}/sqlcontainers/{coll}/docs/</c>
    /// The directory is created if it does not already exist.
    /// </summary>
    internal string GetDocumentDirectory(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string accountName,
        string databaseName,
        string collectionName)
    {
        var basePath = GetServiceInstancePath(sub, rg, null);
        var docsDir = Path.Combine(
            basePath,
            accountName,
            SqlDatabasesSubresource,
            databaseName,
            SqlContainersSubresource,
            collectionName,
            DocsSubdirectory);

        if (!Directory.Exists(docsDir))
            Directory.CreateDirectory(docsDir);

        return docsDir;
    }
}
