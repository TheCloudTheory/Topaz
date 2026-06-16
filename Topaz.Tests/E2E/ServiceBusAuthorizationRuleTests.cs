using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusAuthorizationRuleTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("C1A2B3D4-E5F6-7890-ABCD-EF1234567890");
    private const string SubscriptionName = "sub-sb-authrule-test";
    private const string ResourceGroupName = "rg-sb-authrule-test";
    private const string NamespaceName = "sb-authrule-test";
    private const string QueueName = "authrule-queue";
    private const string TopicName = "authrule-topic";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "queue", "create", "--name", QueueName, "--namespace-name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "topic", "create", "--name", TopicName, "--namespace-name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private (ArmClient arm, AzureLocalCredential cred) CreateClient()
    {
        var cred = new AzureLocalCredential(Globals.GlobalAdminId);
        return (new ArmClient(cred, SubscriptionId.ToString(), ArmClientOptions), cred);
    }

    private async Task<ServiceBusNamespaceResource> GetNamespace(ArmClient armClient)
    {
        var sub = await armClient.GetDefaultSubscriptionAsync();
        var rg = await sub.GetResourceGroupAsync(ResourceGroupName);
        return (await rg.Value.GetServiceBusNamespaces().GetAsync(NamespaceName)).Value;
    }

    // ── Namespace authorization rules ──────────────────────────────────────

    [Test]
    public async Task Namespace_CreateOrUpdate_AuthorizationRule_ShouldPersistWithGeneratedKeys()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);

        const string ruleName = "TestRule";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Listen);
        data.Rights.Add(ServiceBusAccessRight.Send);

        var rule = (await ns.GetServiceBusNamespaceAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data)).Value;

        Assert.Multiple(() =>
        {
            Assert.That(rule.Data.Name, Is.EqualTo(ruleName));
            Assert.That(rule.Data.Rights, Contains.Item(ServiceBusAccessRight.Listen));
            Assert.That(rule.Data.Rights, Contains.Item(ServiceBusAccessRight.Send));
        });
    }

    [Test]
    public async Task Namespace_ListKeys_ShouldReturnRealKeys()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);

        // RootManageSharedAccessKey is auto-created on namespace creation
        var rule = (await ns.GetServiceBusNamespaceAuthorizationRules().GetAsync("RootManageSharedAccessKey")).Value;
        var keys = (await rule.GetKeysAsync()).Value;

        Assert.Multiple(() =>
        {
            Assert.That(keys.PrimaryKey, Is.Not.EqualTo("SAS_KEY_VALUE"));
            Assert.That(keys.SecondaryKey, Is.Not.EqualTo("SAS_KEY_VALUE"));
            Assert.That(keys.PrimaryConnectionString, Does.Contain("SharedAccessKey="));
            Assert.That(keys.PrimaryConnectionString, Does.Not.Contain("SAS_KEY_VALUE"));
        });
    }

    [Test]
    public async Task Namespace_RegenerateKeys_Primary_ShouldChangePrimaryOnly()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);
        var rule = (await ns.GetServiceBusNamespaceAuthorizationRules().GetAsync("RootManageSharedAccessKey")).Value;

        var before = (await rule.GetKeysAsync()).Value;
        await rule.RegenerateKeysAsync(new ServiceBusRegenerateAccessKeyContent(ServiceBusAccessKeyType.PrimaryKey));
        var after = (await rule.GetKeysAsync()).Value;

        Assert.Multiple(() =>
        {
            Assert.That(after.PrimaryKey, Is.Not.EqualTo(before.PrimaryKey));
            Assert.That(after.SecondaryKey, Is.EqualTo(before.SecondaryKey));
        });
    }

    [Test]
    public async Task Namespace_DeleteAuthorizationRule_ShouldNotBeFoundAfterDelete()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);

        const string ruleName = "DeleteMe";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Listen);
        await ns.GetServiceBusNamespaceAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data);

        var rule = (await ns.GetServiceBusNamespaceAuthorizationRules().GetAsync(ruleName)).Value;
        await rule.DeleteAsync(WaitUntil.Completed);

        Assert.That(async () => await ns.GetServiceBusNamespaceAuthorizationRules().GetAsync(ruleName),
            Throws.Exception);
    }

    [Test]
    public async Task Namespace_ListAuthorizationRules_ShouldIncludeDefaultRule()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);

        var rules = new List<ServiceBusNamespaceAuthorizationRuleResource>();
        await foreach (var r in ns.GetServiceBusNamespaceAuthorizationRules())
            rules.Add(r);

        Assert.That(rules.Any(r => r.Data.Name == "RootManageSharedAccessKey"), Is.True);
    }

    // ── Queue authorization rules ──────────────────────────────────────────

    [Test]
    public async Task Queue_CreateOrUpdate_AuthorizationRule_ShouldSucceed()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);
        var queue = (await ns.GetServiceBusQueues().GetAsync(QueueName)).Value;

        const string ruleName = "QueueRule";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Listen);

        var rule = (await queue.GetServiceBusQueueAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data)).Value;
        Assert.That(rule.Data.Name, Is.EqualTo(ruleName));
    }

    [Test]
    public async Task Queue_ListKeys_ShouldReturnConnectionString()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);
        var queue = (await ns.GetServiceBusQueues().GetAsync(QueueName)).Value;

        const string ruleName = "QueueKeysRule";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Send);
        await queue.GetServiceBusQueueAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data);

        var rule = (await queue.GetServiceBusQueueAuthorizationRules().GetAsync(ruleName)).Value;
        var keys = (await rule.GetKeysAsync()).Value;

        Assert.That(keys.PrimaryConnectionString, Does.Contain("SharedAccessKey="));
    }

    // ── Topic authorization rules ──────────────────────────────────────────

    [Test]
    public async Task Topic_CreateOrUpdate_AuthorizationRule_ShouldSucceed()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);
        var topic = (await ns.GetServiceBusTopics().GetAsync(TopicName)).Value;

        const string ruleName = "TopicRule";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Send);

        var rule = (await topic.GetServiceBusTopicAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data)).Value;
        Assert.That(rule.Data.Name, Is.EqualTo(ruleName));
    }

    [Test]
    public async Task Topic_RegenerateKeys_Secondary_ShouldChangeSecondaryOnly()
    {
        var (armClient, _) = CreateClient();
        var ns = await GetNamespace(armClient);
        var topic = (await ns.GetServiceBusTopics().GetAsync(TopicName)).Value;

        const string ruleName = "TopicRegenRule";
        var data = new ServiceBusAuthorizationRuleData();
        data.Rights.Add(ServiceBusAccessRight.Send);
        await topic.GetServiceBusTopicAuthorizationRules().CreateOrUpdateAsync(WaitUntil.Completed, ruleName, data);

        var rule = (await topic.GetServiceBusTopicAuthorizationRules().GetAsync(ruleName)).Value;
        var before = (await rule.GetKeysAsync()).Value;

        await rule.RegenerateKeysAsync(new ServiceBusRegenerateAccessKeyContent(ServiceBusAccessKeyType.SecondaryKey));
        var after = (await rule.GetKeysAsync()).Value;

        Assert.Multiple(() =>
        {
            Assert.That(after.PrimaryKey, Is.EqualTo(before.PrimaryKey));
            Assert.That(after.SecondaryKey, Is.Not.EqualTo(before.SecondaryKey));
        });
    }
}
