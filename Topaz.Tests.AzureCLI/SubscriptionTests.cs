using Topaz.Shared;

namespace Topaz.Tests.AzureCLI;

public class SubscriptionTests : TopazFixture
{
    [Test]
    public async Task SubscriptionTests_WhenAccountShowIsCalled_TenantIdShouldBeAvailable()
    {
        await RunAzureCliCommand("az account show", response =>
        {
            Assert.That(response["tenantId"]?.GetValue<string>(),
                Is.EqualTo(GlobalSettings.DefaultTenantId).IgnoreCase);
        });
    }
}
