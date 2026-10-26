using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E.StorageAccount;

internal sealed class AzureDataLakeStorageTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("075A1FEB-A765-4170-899F-B9370412CC9D");
    
    private const string SubscriptionName = "sub-adls-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "adlsstoragetests";

    private string _key = null!;
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "storage",
            "account",
            "create",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
}