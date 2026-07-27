using System.Text.Json.Nodes;
using NSubstitute;
using NUnit.Framework;
using Topaz.Service.CosmosDb;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Tests.Unit.CosmosDb;

[TestFixture]
public class ExpiredDocumentsPurgeSchedulerTests
{
    private ICosmosDbDataPlane _dataPlane = null!;
    private ICosmosDbControlPlane _controlPlane = null!;
    private ISubscriptionLister _subscriptionLister = null!;
    private ExpiredDocumentsPurgeScheduler _scheduler = null!;

    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly string DbId = "testdb";
    private static readonly string ContainerId = "testcontainer";

    [SetUp]
    public void SetUp()
    {
        _dataPlane = Substitute.For<ICosmosDbDataPlane>();
        _controlPlane = Substitute.For<ICosmosDbControlPlane>();
        _subscriptionLister = Substitute.For<ISubscriptionLister>();

        var logger = Substitute.For<ITopazLogger>();
        _scheduler = new ExpiredDocumentsPurgeScheduler(
            _dataPlane, _controlPlane, _subscriptionLister, logger, TimeSpan.FromMinutes(1));

        // Wire up subscription → account → database → container chain
        var subscription = new Topaz.Service.Subscription.Models.Subscription
        {
            SubscriptionId = SubscriptionId.ToString()
        };

        _subscriptionLister.List()
            .Returns(new ControlPlaneOperationResult<Topaz.Service.Subscription.Models.Subscription[]>(
                OperationResult.Success, [subscription], null, null));

        var account = new DatabaseAccountResource(
            SubscriptionIdentifier.From(SubscriptionId),
            ResourceGroupIdentifier.From("rg"),
            "account",
            Azure.Core.AzureLocation.EastUS,
            null,
            new DatabaseAccountResourceProperties());

        _controlPlane.ListBySubscription(Arg.Any<SubscriptionIdentifier>())
            .Returns(new ControlPlaneOperationResult<DatabaseAccountResource[]>(
                OperationResult.Success, [account], null, null));

        _dataPlane.ListDatabases(Arg.Any<CosmosDbAccountContext>())
            .Returns(new DataPlaneOperationResult<SqlDatabaseInnerResource[]>(
                OperationResult.Success, [new SqlDatabaseInnerResource { Id = DbId }], null, null));
    }

    private void SetupContainer(int? defaultTtl) =>
        _dataPlane.ListCollections(Arg.Any<CosmosDbAccountContext>(), DbId)
            .Returns(new DataPlaneOperationResult<SqlContainerInnerResource[]>(
                OperationResult.Success,
                [new SqlContainerInnerResource { Id = ContainerId, DefaultTtl = defaultTtl }],
                null, null));

    private void SetupDocuments(params JsonObject[] documents) =>
        _dataPlane.ListDocuments(Arg.Any<CosmosDbAccountContext>(), DbId, ContainerId)
            .Returns(new DataPlaneOperationResult<JsonObject[]>(
                OperationResult.Success, documents, null, null));

    private static JsonObject DocWithoutTtl(string id = "doc-1") =>
        new() { ["id"] = id, ["_ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

    private static JsonObject DocWithTtl(string id, int ttl, long ts) =>
        new() { ["id"] = id, ["ttl"] = ttl, ["_ts"] = ts };

    [Test]
    public async Task NoDefaultTtl_NoTtlDocs_NothingDeleted()
    {
        SetupContainer(defaultTtl: null);
        SetupDocuments(DocWithoutTtl());

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.DidNotReceive().DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task NoDefaultTtl_DocWithTtl_NotExpired_NotDeleted()
    {
        SetupContainer(defaultTtl: null);
        // _ts = now, ttl = 3600 → expires in 1 hour
        SetupDocuments(DocWithTtl("doc-1", ttl: 3600, ts: DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.DidNotReceive().DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task NoDefaultTtl_DocWithTtl_Expired_Deleted()
    {
        SetupContainer(defaultTtl: null);
        // _ts = 1 hour ago, ttl = 60 → expired 59 minutes ago
        var expiredTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
        SetupDocuments(DocWithTtl("doc-1", ttl: 60, ts: expiredTs));

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.Received(1).DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            DbId, ContainerId, "doc-1",
            string.Empty, null);
    }

    [Test]
    public async Task DefaultTtl_DocWithoutTtl_Deleted()
    {
        SetupContainer(defaultTtl: 300);
        SetupDocuments(DocWithoutTtl("doc-1"));

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.Received(1).DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            DbId, ContainerId, "doc-1",
            string.Empty, null);
    }

    [Test]
    public async Task DefaultTtl_DocWithTtl_NotExpired_NotDeleted()
    {
        SetupContainer(defaultTtl: 300);
        SetupDocuments(DocWithTtl("doc-1", ttl: 3600, ts: DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.DidNotReceive().DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            Arg.Any<string>(), Arg.Any<string>(), "doc-1",
            Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task DefaultTtl_DocWithTtl_Expired_Deleted()
    {
        SetupContainer(defaultTtl: 300);
        var expiredTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
        SetupDocuments(DocWithTtl("doc-1", ttl: 60, ts: expiredTs));

        await _scheduler.ScanAndUpdateAsync();

        _dataPlane.Received(1).DeleteDocument(
            Arg.Any<CosmosDbAccountContext>(),
            DbId, ContainerId, "doc-1",
            string.Empty, null);
    }
}
