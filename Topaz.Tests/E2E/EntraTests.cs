using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Topaz.Tests.E2E;

public class EntraTests
{
    private static GraphServiceClient GraphClient => new(new HttpClient(), null, "https://topaz.local.dev:8899");
    
    [Test]
    public async Task EntraTests_CanAuthenticateToEmulatedTenant()
    {
        // Arrange
        var client = GraphClient;
        
        // Act
        var me = await client.Me.GetAsync();
        
        // Assert
        Assert.That(me, Is.Not.Null);
    }

    [Test]
    public async Task EntraTests_CanCreateAndFindUser()
    {
        // Prepare a unique user
        var client = GraphClient;
        var unique = Guid.NewGuid().ToString("N");
        var upn = $"{unique}@example.com";
        var user = new User
        {
            AccountEnabled = true,
            DisplayName = $"Test User {unique}",
            MailNickname = $"test{unique}",
            UserPrincipalName = upn,
            PasswordProfile = new PasswordProfile
            {
                Password = "P@ssw0rd123!",
                ForceChangePasswordNextSignIn = false
            }
        };

        // Create
        var created = await client.Users.PostAsync(user);
        Assert.That(created, Is.Not.Null);

        try
        {
            // Retrieve by id
            var found = await client.Users[created.Id].GetAsync();
            Assert.That(found, Is.Not.Null);
            Assert.That(found.UserPrincipalName, Is.EqualTo(upn));
        }
        finally
        {
            // Clean up
            if (created?.Id is not null)
            {
                await client.Users[created.Id].DeleteAsync();
            }
        }
    }
}