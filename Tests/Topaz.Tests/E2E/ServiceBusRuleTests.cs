using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusRuleTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("BF5A9E7C-1234-4D2A-9B3E-7F8A0C1D2E5F");

    private const string SubscriptionName = "sub-rules-test";
    private const string ResourceGroupName = "rg-rules-test";
    private const string NamespaceName = "sb-rules-test";
    private const string TopicName = "rules-topic";
    private const string TopicSubscriptionName = "rules-subscription";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var client = new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        await client.CreateTopicAsync(TopicName);
        await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, TopicSubscriptionName));
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ServiceBusAdministrationClient CreateClient() =>
        new(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));

    [Test]
    public async Task CreateSubscription_ShouldAutoCreateDefaultTrueRule()
    {
        // Arrange
        var client = CreateClient();

        // Act — subscription was already created in SetUp
        var rules = client.GetRulesAsync(TopicName, TopicSubscriptionName);
        var ruleList = new List<RuleProperties>();
        await foreach (var rule in rules)
            ruleList.Add(rule);

        // Assert
        Assert.That(ruleList, Has.Count.EqualTo(1));
        Assert.That(ruleList[0].Name, Is.EqualTo("$Default"));
        Assert.That(ruleList[0].Filter, Is.InstanceOf<TrueRuleFilter>());
    }

    [Test]
    public async Task CreateRule_TrueFilter_ShouldSucceed()
    {
        // Arrange
        var client = CreateClient();
        const string ruleName = "true-rule";

        // Act
        var created = await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(ruleName, new TrueRuleFilter()));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(created.Value.Name, Is.EqualTo(ruleName));
            Assert.That(created.Value.Filter, Is.InstanceOf<TrueRuleFilter>());
        });

        // Cleanup
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, ruleName);
    }

    [Test]
    public async Task CreateRule_SqlFilter_ShouldSucceed()
    {
        // Arrange
        var client = CreateClient();
        const string ruleName = "sql-rule";
        const string sqlExpression = "color = 'red'";

        // Act
        var created = await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(ruleName, new SqlRuleFilter(sqlExpression)));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(created.Value.Name, Is.EqualTo(ruleName));
            Assert.That(created.Value.Filter, Is.InstanceOf<SqlRuleFilter>());
            Assert.That(((SqlRuleFilter)created.Value.Filter).SqlExpression, Is.EqualTo(sqlExpression));
        });

        // Cleanup
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, ruleName);
    }

    [Test]
    public async Task CreateRule_CorrelationFilter_ShouldSucceed()
    {
        // Arrange
        var client = CreateClient();
        const string ruleName = "correlation-rule";

        // Act
        var created = await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(ruleName, new CorrelationRuleFilter { CorrelationId = "order-placed" }));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(created.Value.Name, Is.EqualTo(ruleName));
            Assert.That(created.Value.Filter, Is.InstanceOf<CorrelationRuleFilter>());
            Assert.That(((CorrelationRuleFilter)created.Value.Filter).CorrelationId, Is.EqualTo("order-placed"));
        });

        // Cleanup
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, ruleName);
    }

    [Test]
    public async Task GetRule_Existing_ShouldReturn200()
    {
        // Arrange
        var client = CreateClient();
        const string ruleName = "get-rule";
        await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(ruleName, new TrueRuleFilter()));

        // Act
        var rule = await client.GetRuleAsync(TopicName, TopicSubscriptionName, ruleName);

        // Assert
        Assert.That(rule.Value.Name, Is.EqualTo(ruleName));

        // Cleanup
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, ruleName);
    }

    [Test]
    public void GetRule_NonExistent_ShouldThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        Assert.ThrowsAsync<Azure.Messaging.ServiceBus.ServiceBusException>(() =>
            client.GetRuleAsync(TopicName, TopicSubscriptionName, "does-not-exist"));
    }

    [Test]
    public async Task DeleteRule_Existing_ShouldSucceed()
    {
        // Arrange
        var client = CreateClient();
        const string ruleName = "delete-rule";
        await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(ruleName, new TrueRuleFilter()));

        // Act
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, ruleName);

        // Assert
        Assert.ThrowsAsync<Azure.Messaging.ServiceBus.ServiceBusException>(() =>
            client.GetRuleAsync(TopicName, TopicSubscriptionName, ruleName));
    }

    [Test]
    public async Task ListRules_ShouldReturnDefaultAndAdded()
    {
        // Arrange
        var client = CreateClient();
        const string sqlRuleName = "list-sql-rule";
        await client.CreateRuleAsync(TopicName, TopicSubscriptionName,
            new CreateRuleOptions(sqlRuleName, new SqlRuleFilter("1=1")));

        // Act
        var rules = client.GetRulesAsync(TopicName, TopicSubscriptionName);
        var ruleList = new List<RuleProperties>();
        await foreach (var rule in rules)
            ruleList.Add(rule);

        // Assert
        Assert.That(ruleList, Has.Count.EqualTo(2));
        Assert.That(ruleList.Select(r => r.Name), Does.Contain("$Default"));
        Assert.That(ruleList.Select(r => r.Name), Does.Contain(sqlRuleName));

        // Cleanup
        await client.DeleteRuleAsync(TopicName, TopicSubscriptionName, sqlRuleName);
    }
}
