namespace Topaz.Tests.AzureCLI;

public class ServiceBusTests : TopazFixture
{
	[Test]
	public async Task ServiceBusNamespace_Create_Show_List_Update_And_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string resourceGroup = "test-rg";
		string namespaceId = null!;

		// Create a resource group first
		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");

		// Create namespace
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus", (resp) => {
            Assert.Multiple(() =>
		    {
		        Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(namespaceName));
		        Assert.That(resp["provisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
		    });
            namespaceId = resp["id"]!.GetValue<string>();
        });

		// Show namespace
		await RunAzureCliCommand($"az servicebus namespace show --resource-group {resourceGroup} --name {namespaceName}", (resp) => {
            Assert.Multiple(() =>
		    {
		        Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(namespaceName));
		        Assert.That(resp["id"]!.GetValue<string>(), Is.EqualTo(namespaceId));
		    });
        });

		// List namespaces in a resource group
		await RunAzureCliCommand($"az servicebus namespace list --resource-group {resourceGroup}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == namespaceName), Is.True);
		});

		// Update namespace (add tags)
		await RunAzureCliCommand($"az servicebus namespace update --resource-group {resourceGroup} --name {namespaceName} --tags environment=test purpose=testing", (resp) => {
            Assert.That(resp["tags"], Is.Not.Null);
			var tags = resp["tags"]!.AsObject();
			
            Assert.Multiple(() =>
            {
                Assert.That(tags["environment"]!.GetValue<string>(), Is.EqualTo("test"));
                Assert.That(tags["purpose"]!.GetValue<string>(), Is.EqualTo("testing"));
            });
        });

		// Delete namespace
		await RunAzureCliCommand($"az servicebus namespace delete --resource-group {resourceGroup} --name {namespaceName}");

		// Verify deletion - namespace should not be in list
		await RunAzureCliCommand($"az servicebus namespace list --resource-group {resourceGroup}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == namespaceName), Is.False);
		});

		// Cleanup resource group
		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}

	[Test]
	public async Task ServiceBusQueue_Create_Show_List_Update_And_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string queueName = "testqueue";
		const string resourceGroup = "test-rg";

		// Create a resource group and namespace
		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus");

		// Create queue
		await RunAzureCliCommand($"az servicebus queue create --resource-group {resourceGroup} --namespace-name {namespaceName} --name {queueName}", (resp) =>
		{
			Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(queueName));
		});

		// Show queue
		await RunAzureCliCommand($"az servicebus queue show --resource-group {resourceGroup} --namespace-name {namespaceName} --name {queueName}", (resp) =>
		{
			Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(queueName));
		});

		// List queues
		await RunAzureCliCommand($"az servicebus queue list --resource-group {resourceGroup} --namespace-name {namespaceName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == queueName), Is.True);
		});

		// Update queue (change max delivery count)
		await RunAzureCliCommand($"az servicebus queue update --resource-group {resourceGroup} --namespace-name {namespaceName} --name {queueName} --max-delivery-count 20", (resp) =>
		{
			Assert.That(resp["maxDeliveryCount"]!.GetValue<int>(), Is.EqualTo(20));
		});

		// Delete queue
		await RunAzureCliCommand($"az servicebus queue delete --resource-group {resourceGroup} --namespace-name {namespaceName} --name {queueName}");

		// Verify deletion
		await RunAzureCliCommand($"az servicebus queue list --resource-group {resourceGroup} --namespace-name {namespaceName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == queueName), Is.False);
		});

		// Cleanup
		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}

	[Test]
	public async Task ServiceBusTopic_Create_Show_List_Update_And_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string topicName = "testtopic";
		const string resourceGroup = "test-rg";

		// Create a resource group and namespace
		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus");

		// Create a topic
		await RunAzureCliCommand($"az servicebus topic create --resource-group {resourceGroup} --namespace-name {namespaceName} --name {topicName}", (resp) =>
		{
			Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(topicName));
		});

		// Show topic
		await RunAzureCliCommand($"az servicebus topic show --resource-group {resourceGroup} --namespace-name {namespaceName} --name {topicName}", (resp) =>
		{
			Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(topicName));
		});

		// List topics
		await RunAzureCliCommand($"az servicebus topic list --resource-group {resourceGroup} --namespace-name {namespaceName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == topicName), Is.True);
		});

		// Update topic (enable partitioning if supported, or change max size)
		await RunAzureCliCommand($"az servicebus topic update --resource-group {resourceGroup} --namespace-name {namespaceName} --name {topicName} --max-size 2048", (resp) =>
		{
			Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(topicName));
		});

		// Delete topic
		await RunAzureCliCommand($"az servicebus topic delete --resource-group {resourceGroup} --namespace-name {namespaceName} --name {topicName}");

		// Verify deletion
		await RunAzureCliCommand($"az servicebus topic list --resource-group {resourceGroup} --namespace-name {namespaceName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == topicName), Is.False);
		});

		// Cleanup
		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}

	[Test]
	public async Task ServiceBusNamespace_AuthorizationRule_Create_ListKeys_Regenerate_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string resourceGroup = "test-rg";
		const string ruleName = "TestRule";

		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus");

		// Create authorization rule
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule create --resource-group {resourceGroup} --namespace-name {namespaceName} --name {ruleName} --rights Listen Send",
			(resp) =>
			{
				Assert.Multiple(() =>
				{
					Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(ruleName));
					Assert.That(resp["rights"]!.AsArray().Select(r => r!.GetValue<string>()),
						Is.SupersetOf(new[] { "Listen", "Send" }));
				});
			});

		// Show rule
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule show --resource-group {resourceGroup} --namespace-name {namespaceName} --name {ruleName}",
			(resp) => Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(ruleName)));

		// List rules — must include our rule and the default RootManageSharedAccessKey
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule list --resource-group {resourceGroup} --namespace-name {namespaceName}",
			(resp) =>
			{
				var arr = resp.AsArray();
				Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == ruleName), Is.True);
				Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == "RootManageSharedAccessKey"), Is.True);
			});

		// List keys — should not contain the stub value
		string primaryKeyBefore = null!;
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule keys list --resource-group {resourceGroup} --namespace-name {namespaceName} --name {ruleName}",
			(resp) =>
			{
				primaryKeyBefore = resp["primaryKey"]!.GetValue<string>();
				Assert.Multiple(() =>
				{
					Assert.That(primaryKeyBefore, Is.Not.EqualTo("SAS_KEY_VALUE"));
					Assert.That(resp["primaryConnectionString"]!.GetValue<string>(), Does.Contain("SharedAccessKey="));
				});
			});

		// Regenerate primary key — primary must change, secondary must stay
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule keys renew --resource-group {resourceGroup} --namespace-name {namespaceName} --name {ruleName} --key PrimaryKey",
			(resp) =>
			{
				Assert.That(resp["primaryKey"]!.GetValue<string>(), Is.Not.EqualTo(primaryKeyBefore));
			});

		// Delete rule
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule delete --resource-group {resourceGroup} --namespace-name {namespaceName} --name {ruleName}");

		// Verify deletion
		await RunAzureCliCommand(
			$"az servicebus namespace authorization-rule list --resource-group {resourceGroup} --namespace-name {namespaceName}",
			(resp) =>
			{
				var arr = resp.AsArray();
				Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == ruleName), Is.False);
			});

		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}

	[Test]
	public async Task ServiceBusQueue_AuthorizationRule_Create_ListKeys_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string resourceGroup = "test-rg";
		const string queueName = "authrule-queue";
		const string ruleName = "QueueRule";

		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus");
		await RunAzureCliCommand($"az servicebus queue create --resource-group {resourceGroup} --namespace-name {namespaceName} --name {queueName}");

		// Create queue auth rule
		await RunAzureCliCommand(
			$"az servicebus queue authorization-rule create --resource-group {resourceGroup} --namespace-name {namespaceName} --queue-name {queueName} --name {ruleName} --rights Listen",
			(resp) => Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(ruleName)));

		// List keys
		await RunAzureCliCommand(
			$"az servicebus queue authorization-rule keys list --resource-group {resourceGroup} --namespace-name {namespaceName} --queue-name {queueName} --name {ruleName}",
			(resp) => Assert.That(resp["primaryConnectionString"]!.GetValue<string>(), Does.Contain("SharedAccessKey=")));

		// Delete
		await RunAzureCliCommand(
			$"az servicebus queue authorization-rule delete --resource-group {resourceGroup} --namespace-name {namespaceName} --queue-name {queueName} --name {ruleName}");

		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}

	[Test]
	public async Task ServiceBusTopic_AuthorizationRule_Create_ListKeys_Delete()
	{
		var namespaceName = $"topazsbns{Guid.NewGuid():N}";
		const string resourceGroup = "test-rg";
		const string topicName = "authrule-topic";
		const string ruleName = "TopicRule";

		await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
		await RunAzureCliCommand($"az servicebus namespace create --resource-group {resourceGroup} --name {namespaceName} --location eastus");
		await RunAzureCliCommand($"az servicebus topic create --resource-group {resourceGroup} --namespace-name {namespaceName} --name {topicName}");

		// Create topic auth rule
		await RunAzureCliCommand(
			$"az servicebus topic authorization-rule create --resource-group {resourceGroup} --namespace-name {namespaceName} --topic-name {topicName} --name {ruleName} --rights Send",
			(resp) => Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(ruleName)));

		// List keys
		await RunAzureCliCommand(
			$"az servicebus topic authorization-rule keys list --resource-group {resourceGroup} --namespace-name {namespaceName} --topic-name {topicName} --name {ruleName}",
			(resp) => Assert.That(resp["primaryConnectionString"]!.GetValue<string>(), Does.Contain("SharedAccessKey=")));

		// Delete
		await RunAzureCliCommand(
			$"az servicebus topic authorization-rule delete --resource-group {resourceGroup} --namespace-name {namespaceName} --topic-name {topicName} --name {ruleName}");

		await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
	}
}