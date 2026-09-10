using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// Subscription rules created through the <b>Resource Manager</b> API, which is the path ARM templates,
/// Bicep and <c>az deployment</c> take.
///
/// <para>
/// <see cref="ServiceBusRuleTests"/> and <see cref="TopicSubscriptionRuleFilteringTests"/> both drive
/// <c>ServiceBusAdministrationClient</c>, which reaches the Atom endpoint. That endpoint converts the
/// request through <c>CreateOrUpdateServiceBusRuleAtomRequest.ToProperties</c> and has always worked; the
/// ARM endpoint had no coverage, and was deserializing the whole <c>{"properties":{…}}</c> envelope into
/// the properties type — so every filter was dropped and the rule fell back to an empty SqlFilter.
/// </para>
/// </summary>
public class ServiceBusRuleArmTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D2B3C4E5-F6A7-8901-BCDE-F23456789012");

    private const string SubscriptionName = "sub-sb-rule-arm-test";
    private const string ResourceGroupName = "rg-sb-rule-arm-test";
    private const string NamespaceName = "sb-rule-arm-test";
    private const string TopicName = "rule-arm-topic";
    private const string TopicSubscriptionName = "rule-arm-subscription";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var adminClient = new ServiceBusAdministrationClient(
            TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        await adminClient.CreateTopicAsync(TopicName);
        await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, TopicSubscriptionName));
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private async Task<ServiceBusRuleCollection> GetRules()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var serviceBusNamespace = (await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName)).Value;
        var topic = (await serviceBusNamespace.GetServiceBusTopics().GetAsync(TopicName)).Value;
        var topicSubscription = (await topic.GetServiceBusSubscriptions().GetAsync(TopicSubscriptionName)).Value;

        return topicSubscription.GetServiceBusRules();
    }

    [Test]
    public async Task CreateRuleViaArm_ShouldPersistCorrelationFilterProperties()
    {
        var rules = await GetRules();

        var data = new ServiceBusRuleData
        {
            FilterType = ServiceBusFilterType.CorrelationFilter,
            CorrelationFilter = new ServiceBusCorrelationFilter()
        };
        data.CorrelationFilter.ApplicationProperties.Add("PayloadType", "InventoryAdjusted");

        await rules.CreateOrUpdateAsync(Azure.WaitUntil.Completed, "correlation-rule", data);

        var stored = (await rules.GetAsync("correlation-rule")).Value.Data;

        Assert.Multiple(() =>
        {
            Assert.That(stored.FilterType, Is.EqualTo(ServiceBusFilterType.CorrelationFilter));
            Assert.That(stored.CorrelationFilter, Is.Not.Null);
            Assert.That(stored.CorrelationFilter.ApplicationProperties["PayloadType"], Is.EqualTo("InventoryAdjusted"));
        });
    }

    [Test]
    public async Task CreateRuleViaArm_ShouldPersistCorrelationFilterSubject()
    {
        var rules = await GetRules();

        var data = new ServiceBusRuleData
        {
            FilterType = ServiceBusFilterType.CorrelationFilter,
            CorrelationFilter = new ServiceBusCorrelationFilter { Subject = "InventoryAdjusted" }
        };

        await rules.CreateOrUpdateAsync(Azure.WaitUntil.Completed, "subject-rule", data);

        var stored = (await rules.GetAsync("subject-rule")).Value.Data;

        Assert.Multiple(() =>
        {
            Assert.That(stored.FilterType, Is.EqualTo(ServiceBusFilterType.CorrelationFilter));
            Assert.That(stored.CorrelationFilter?.Subject, Is.EqualTo("InventoryAdjusted"));
        });
    }

    [Test]
    public async Task CreateRuleViaArm_ShouldPersistSqlFilterExpression()
    {
        var rules = await GetRules();

        var data = new ServiceBusRuleData
        {
            FilterType = ServiceBusFilterType.SqlFilter,
            SqlFilter = new ServiceBusSqlFilter { SqlExpression = "PayloadType = 'InventoryAdjusted'" }
        };

        await rules.CreateOrUpdateAsync(Azure.WaitUntil.Completed, "sql-rule", data);

        var stored = (await rules.GetAsync("sql-rule")).Value.Data;

        Assert.Multiple(() =>
        {
            Assert.That(stored.FilterType, Is.EqualTo(ServiceBusFilterType.SqlFilter));
            Assert.That(stored.SqlFilter?.SqlExpression, Is.EqualTo("PayloadType = 'InventoryAdjusted'"));
        });
    }
}
