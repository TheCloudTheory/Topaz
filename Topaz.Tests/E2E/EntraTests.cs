using Microsoft.Graph;

namespace Topaz.Tests.E2E;

public class EntraTests
{
    [Test]
    public async Task EntraTests_CanAuthenticateToEmulatedTenant()
    {
        // Arrange
        var client = new GraphServiceClient(new HttpClient(), null, "https://topaz.local.dev:8899");
        
        // Act
        var me = await client.Me.GetAsync();
        
        // Assert
        Assert.That(me, Is.Not.Null);
    }
}