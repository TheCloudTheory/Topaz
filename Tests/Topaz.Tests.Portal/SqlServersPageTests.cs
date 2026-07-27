using Topaz.Portal.Components.Pages.Sql;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.Sql;

namespace Topaz.Tests.Portal;

[TestFixture]
public class SqlServersPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void SqlServersPage_EmptyList_ShowsNoSqlServersMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListSqlServers(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListSqlServersResponse { Value = [] }));

        var cut = Render<SqlServers>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No SQL servers found")));
    }
}

[TestFixture]
public class SqlServersPage_WithServers_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void SqlServersPage_WithServers_ShowsTable()
    {
        var subscriptionId = Guid.NewGuid();

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subscriptionId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListSqlServers(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListSqlServersResponse
            {
                Value =
                [
                    new SqlServerDto
                    {
                        Id = $"/subscriptions/{subscriptionId:D}/resourceGroups/rg1/providers/Microsoft.Sql/servers/sql-one",
                        Name = "sql-one",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subscriptionId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        State = "Ready",
                        Version = "12.0"
                    }
                ]
            }));

        var cut = Render<SqlServers>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("sql-one")), Is.True,
                "Expected the SQL server name to appear in the table.");
        });
    }
}

[TestFixture]
public class SqlServersPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task SqlServersPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subscriptionId = Guid.NewGuid();
        const string serverName = "sql-new-server";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subscriptionId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListSqlServers(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ListSqlServersResponse { Value = [] }),
                Task.FromResult(new ListSqlServersResponse
                {
                    Value =
                    [
                        new SqlServerDto
                        {
                            Name = serverName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subscriptionId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            State = "Ready",
                            Version = "12.0"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateSqlServer(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<SqlServers>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No SQL servers found")));

        cut.Find("button.btn-primary").Click();

        cut.Find("select").Change(subscriptionId.ToString("D"));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        var selects = cut.FindAll("select");
        selects[1].Change("rg1");

        cut.Find("input[placeholder='e.g. my-sql-server']").Change(serverName);
        cut.Find("input[placeholder='e.g. sqladmin']").Change("sqladmin");
        cut.Find("input[placeholder='Enter admin password']").Change("SqlAdmin1234!@#");

        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(serverName)), Is.True,
                "Expected the new SQL server name to appear in the table.");
        });

        await _client.Received(1).CreateSqlServer(
            Arg.Is<Guid>(g => g == subscriptionId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == serverName),
            Arg.Any<string>(),
            Arg.Is<string>(s => s == "sqladmin"),
            Arg.Is<string>(s => s == "SqlAdmin1234!@#"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
