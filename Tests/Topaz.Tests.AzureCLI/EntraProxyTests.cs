namespace Topaz.Tests.AzureCLI;

/// <summary>
/// Tests that az login --username --password (ROPC flow) works via the built-in CONNECT proxy
/// on a non-Docker local install where port 443 is not bound by Topaz.
/// MSAL's user-realm discovery pre-flight targets port 443; the proxy remaps it to port 8899.
/// </summary>
public class EntraProxyTests : TopazProxyFixture
{
    [Test]
    public async Task User_Created_With_AzureCli_Can_Login_With_Username_And_Password_Via_ConnectProxy()
    {
        var upn = $"topaz-proxy-{Guid.NewGuid():N}@mytenant.onmicrosoft.com";
        const string password = "P@ssw0rd!";
        string createdId = null!;

        try
        {
            await RunAzureCliCommand(
                $"az ad user create --display-name \"Topaz Proxy Test User\" --user-principal-name \"{upn}\" --password \"{password}\" --force-change-password-next-sign-in false -o json",
                (resp) =>
                {
                    Assert.That(resp["id"], Is.Not.Null);
                    createdId = resp["id"]!.GetValue<string>();
                    Assert.That(resp["userPrincipalName"]!.GetValue<string>(), Is.EqualTo(upn));
                });

            // Ensure we are not accidentally using the admin session from OneTimeSetUp.
            await RunAzureCliCommand("az logout");

            // Login via ROPC — MSAL will issue GET /common/userrealm/{upn}?api-version=1.0 to
            // port 443, which the CONNECT proxy remaps to port 8899 where Topaz handles it.
            await RunAzureCliCommand(
                $"az login --username \"{upn}\" --password \"{password}\" --allow-no-subscriptions -o json");

            // Verify the signed-in identity matches the created user.
            await RunAzureCliCommand(
                "az ad signed-in-user show -o json",
                (resp) =>
                {
                    Assert.That(resp["userPrincipalName"], Is.Not.Null);
                    Assert.That(resp["userPrincipalName"]!.GetValue<string>(), Is.EqualTo(upn));
                });
        }
        finally
        {
            // Restore admin login for any subsequent tests in this fixture.
            await RunAzureCliCommand("az logout || true");
            await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");

            if (!string.IsNullOrEmpty(createdId))
            {
                await RunAzureCliCommand($"az ad user delete --id {createdId}");
            }
        }
    }
}
