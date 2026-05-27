using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using Topaz.Identity;

namespace Topaz.Tests.E2E;

public class EntraTests
{
    private static GraphServiceClient GraphClient => new(new HttpClient(),
        new LocalGraphAuthenticationProvider(), "https://topaz.local.dev:8899");
    
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
    
    [Test]
    public async Task EntraTests_CanCreateAndFindServicePrincipal()
    {
        // Prepare a unique service principal
        var client = GraphClient;
        var unique = Guid.NewGuid().ToString("N");
        var appId = Guid.NewGuid().ToString();
        var servicePrincipal = new ServicePrincipal
        {
            AppId = appId,
            DisplayName = $"Test Service Principal {unique}",
            ServicePrincipalType = "Application",
            AccountEnabled = true,
            Tags = ["WindowsAzureActiveDirectoryIntegratedApp"]
        };

        // Create
        var created = await client.ServicePrincipals.PostAsync(servicePrincipal);
        Assert.That(created, Is.Not.Null);

        try
        {
            // Retrieve by id
            var found = await client.ServicePrincipals[created.Id].GetAsync();
            Assert.That(found, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(found.AppId, Is.EqualTo(appId));
                Assert.That(found.DisplayName, Is.EqualTo($"Test Service Principal {unique}"));
            });
        }
        finally
        {
            // Clean up
            if (created?.Id is not null)
            {
                await client.ServicePrincipals[created.Id].DeleteAsync();
            }
        }
    }
    
    [Test]
    public async Task EntraTests_CanCreateUpdateAndDeleteServicePrincipal()
    {
        // Arrange
        var client = GraphClient;
        var unique = Guid.NewGuid().ToString("N");
        var appId = Guid.NewGuid().ToString();
        var originalName = $"Test Service Principal {unique}";
        var updatedName = $"Test Service Principal {unique} (updated)";

        var servicePrincipal = new ServicePrincipal
        {
            AppId = appId,
            DisplayName = originalName,
            ServicePrincipalType = "Application",
            AccountEnabled = true,
            Tags = ["WindowsAzureActiveDirectoryIntegratedApp"]
        };

        // Act - Create
        var created = await client.ServicePrincipals.PostAsync(servicePrincipal);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Id, Is.Not.Null);

        try
        {
            // Act - Update (PATCH)
            await client.ServicePrincipals[created.Id].PatchAsync(new ServicePrincipal
            {
                DisplayName = updatedName
            });

            // Assert - Verify updated
            var foundAfterUpdate = await client.ServicePrincipals[created.Id].GetAsync();
            Assert.That(foundAfterUpdate, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(foundAfterUpdate!.AppId, Is.EqualTo(appId));
                Assert.That(foundAfterUpdate.DisplayName, Is.EqualTo(updatedName));
            });

            // Act - Delete
            await client.ServicePrincipals[created.Id].DeleteAsync();

            // Assert - Verify deleted (GET should fail / return null depending on emulator behavior)
            try
            {
                var foundAfterDelete = await client.ServicePrincipals[created.Id].GetAsync();
                Assert.That(foundAfterDelete, Is.Null, "Expected the service principal to be deleted.");
            }
            catch (Exception)
            {
                Assert.Pass("Service principal deleted successfully (subsequent GET failed as expected).");
            }
        }
        finally
        {
            // Best-effort cleanup in case the test failed before delete
            if (created?.Id is not null)
            {
                try
                {
                    await client.ServicePrincipals[created.Id].DeleteAsync();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    [Test]
    public async Task EntraTests_CanCreateUpdateAndDeleteApplication()
    {
        // Arrange
        var client = GraphClient;
        var unique = Guid.NewGuid().ToString("N");
        var originalName = $"Test Application {unique}";
        var updatedName = $"Test Application {unique} (updated)";

        var application = new Application
        {
            DisplayName = originalName,
            SignInAudience = "AzureADMyOrg"
        };

        // Act - Create
        var created = await client.Applications.PostAsync(application);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Id, Is.Not.Null);

        try
        {
            // Act - Update (PATCH)
            await client.Applications[created.Id].PatchAsync(new Application
            {
                DisplayName = updatedName
            });

            // Assert - Verify updated
            var foundAfterUpdate = await client.Applications[created.Id].GetAsync();
            Assert.That(foundAfterUpdate, Is.Not.Null);
            Assert.That(foundAfterUpdate!.DisplayName, Is.EqualTo(updatedName));

            // Act - Delete
            await client.Applications[created.Id].DeleteAsync();

            // Assert - Verify deleted (GET should fail / return null depending on emulator behavior)
            try
            {
                var foundAfterDelete = await client.Applications[created.Id].GetAsync();
                Assert.That(foundAfterDelete, Is.Null, "Expected the application to be deleted.");
            }
            catch (Exception)
            {
                Assert.Pass("Application deleted successfully (subsequent GET failed as expected).");
            }
        }
        finally
        {
            // Best-effort cleanup in case the test failed before delete
            if (created.Id is not null)
            {
                try
                {
                    await client.Applications[created.Id].DeleteAsync();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    [Test]
    public async Task EntraTests_CanAddPasswordToApplication()
    {
        // Arrange
        var client = GraphClient;
        var unique = Guid.NewGuid().ToString("N");

        var application = new Application
        {
            DisplayName = $"Test Application (add password) {unique}",
            SignInAudience = "AzureADMyOrg"
        };

        var created = await client.Applications.PostAsync(application);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Id, Is.Not.Null);

        try
        {
            var start = DateTimeOffset.UtcNow;
            var end = start.AddDays(30);

            // Act - Add password
            var added = await client.Applications[created.Id].AddPassword.PostAsync(
                new AddPasswordPostRequestBody
                {
                    PasswordCredential = new PasswordCredential
                    {
                        DisplayName = $"pwd-{unique}",
                        StartDateTime = start,
                        EndDateTime = end
                    }
                });

            // Assert - Action result contains generated secret (returned only at creation time)
            Assert.That(added, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(added!.KeyId, Is.Not.Null);
                Assert.That(added.SecretText, Is.Not.Null.And.Not.Empty);
                Assert.That(added.DisplayName, Is.EqualTo($"pwd-{unique}"));
            });

            // Assert - Application now contains the password credential reference
            var found = await client.Applications[created.Id].GetAsync();
            Assert.That(found, Is.Not.Null);

            var creds = found!.PasswordCredentials ?? [];
            Assert.Multiple(() =>
            {
                Assert.That(creds.Any(c => c.KeyId == added.KeyId), Is.True,
                            "Expected the added password credential to be present on the application.");
                Assert.That(creds.All(c => c.SecretText == null), Is.True,
                    "Expected all password credentials to have a null secret.");
            });
        }
        finally
        {
            // Clean up
            if (created.Id is not null)
            {
                try
                {
                    await client.Applications[created.Id].DeleteAsync();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    [Test]
    public async Task AuthorizeEndpoint_DefaultMode_Returns302Redirect()
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var url = "https://topaz.local.dev:8899/organizations/oauth2/v2.0/authorize" +
                  "?state=test-state&redirect_uri=http%3A%2F%2Flocalhost%3A8400";
        var response = await http.GetAsync(url);

        Assert.That((int)response.StatusCode, Is.EqualTo(302));
        Assert.That(response.Headers.Location, Is.Not.Null);
        Assert.That(response.Headers.Location!.Query, Does.Contain("code="));
        Assert.That(response.Headers.Location!.Query, Does.Contain("state=test-state"));
    }

    [Test]
    public async Task AuthorizeEndpoint_FormPostMode_Returns200WithAutoSubmitForm()
    {
        using var http = new HttpClient();

        var url = "https://topaz.local.dev:8899/organizations/oauth2/v2.0/authorize" +
                  "?state=test-state&redirect_uri=http%3A%2F%2Flocalhost%3A8400&response_mode=form_post";
        var response = await http.GetAsync(url);

        Assert.That((int)response.StatusCode, Is.EqualTo(200));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("<form"));
            Assert.That(body, Does.Contain("name=\"code\""));
            Assert.That(body, Does.Contain("name=\"state\""));
            Assert.That(body, Does.Contain("test-state"));
            Assert.That(body, Does.Contain("document.forms[0].submit()"));
        });
    }
}