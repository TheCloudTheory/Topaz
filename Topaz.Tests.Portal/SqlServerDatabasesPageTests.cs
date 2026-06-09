using Topaz.Portal.Components.Pages.Sql;
using Topaz.Portal.Models.Sql;

namespace Topaz.Tests.Portal;

[TestFixture]
public class SqlServerDatabasesPage_Load_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void SqlServerDatabasesPage_WithDatabases_ShowsTable()
    {
        var subscriptionId = Guid.NewGuid();

        _client.ListSqlDatabases(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListSqlDatabasesResponse
            {
                Value =
                [
                    new SqlDatabaseDto
                    {
                        Name = "appdb",
                        Status = "Online",
                        Location = "westeurope",
                        Collation = "SQL_Latin1_General_CP1_CI_AS",
                        MaxSizeBytes = 2147483648
                    }
                ]
            }));

        var cut = RenderComponent<SqlServerDatabases>(parameters => parameters
            .Add(p => p.SubscriptionId, subscriptionId)
            .Add(p => p.ResourceGroupName, "rg1")
            .Add(p => p.ServerName, "sql-one"));

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("appdb")), Is.True,
                "Expected the database name to appear in the table.");
        });
    }
}

[TestFixture]
public class SqlServerDatabasesPage_CreateDelete_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task SqlServerDatabasesPage_CreateAndDelete_InvokesClientAndRefreshesList()
    {
        var subscriptionId = Guid.NewGuid();

        _client.ListSqlDatabases(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ListSqlDatabasesResponse { Value = [] }),
                Task.FromResult(new ListSqlDatabasesResponse
                {
                    Value =
                    [
                        new SqlDatabaseDto
                        {
                            Name = "appdb",
                            Status = "Online",
                            Location = "westeurope",
                            Collation = "SQL_Latin1_General_CP1_CI_AS",
                            MaxSizeBytes = 2147483648
                        }
                    ]
                }),
                Task.FromResult(new ListSqlDatabasesResponse { Value = [] }));

        _client.CreateSqlDatabase(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _client.DeleteSqlDatabase(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<SqlServerDatabases>(parameters => parameters
            .Add(p => p.SubscriptionId, subscriptionId)
            .Add(p => p.ResourceGroupName, "rg1")
            .Add(p => p.ServerName, "sql-one"));

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No SQL databases found")));

        cut.Find("button.btn-primary").Click();
        cut.Find("input[placeholder='e.g. appdb']").Change("appdb");
        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("appdb")), Is.True,
                "Expected the created database to appear in the table.");
        });

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No SQL databases found")));

        await _client.Received(1).CreateSqlDatabase(
            Arg.Is<Guid>(g => g == subscriptionId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == "sql-one"),
            Arg.Is<string>(s => s == "appdb"),
            Arg.Any<CancellationToken>());

        await _client.Received(1).DeleteSqlDatabase(
            Arg.Is<Guid>(g => g == subscriptionId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == "sql-one"),
            Arg.Is<string>(s => s == "appdb"),
            Arg.Any<CancellationToken>());
    }
}
