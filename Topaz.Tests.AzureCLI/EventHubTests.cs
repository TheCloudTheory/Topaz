namespace Topaz.Tests.AzureCLI;

public class EventHubTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-eventhub";
    private const string NamespaceName = "ns-cli-eventhub";
    private const string HubName = "hub-cli-test";

    [Test]
    public async Task EventHubTests_WhenEventHubIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az eventhubs namespace create --name {NamespaceName} --resource-group {ResourceGroup} --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub create --name {HubName} --namespace-name {NamespaceName} --resource-group {ResourceGroup}",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub show --name {HubName} --namespace-name {NamespaceName} --resource-group {ResourceGroup}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(HubName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.EventHub/namespaces/eventhubs").IgnoreCase);
                    Assert.That(response["properties"]!["status"]!.GetValue<string>(),
                        Is.EqualTo("Active"));
                });
            }, 0);
    }

    [Test]
    public async Task EventHubTests_WhenEventHubIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az eventhubs namespace create --name {NamespaceName}-del --resource-group {ResourceGroup}-del --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub create --name {HubName}-del --namespace-name {NamespaceName}-del --resource-group {ResourceGroup}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub delete --name {HubName}-del --namespace-name {NamespaceName}-del --resource-group {ResourceGroup}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub show --name {HubName}-del --namespace-name {NamespaceName}-del --resource-group {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task EventHubTests_WhenEventHubsAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az eventhubs namespace create --name {NamespaceName}-list --resource-group {ResourceGroup}-list --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub create --name {HubName}-list-a --namespace-name {NamespaceName}-list --resource-group {ResourceGroup}-list",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub create --name {HubName}-list-b --namespace-name {NamespaceName}-list --resource-group {ResourceGroup}-list",
            null, 0);
        await RunAzureCliCommand(
            $"az eventhubs eventhub list --namespace-name {NamespaceName}-list --resource-group {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{HubName}-list-a"));
                    Assert.That(names, Does.Contain($"{HubName}-list-b"));
                });
            }, 0);
    }
}
