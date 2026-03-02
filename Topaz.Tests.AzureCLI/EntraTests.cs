namespace Topaz.Tests.AzureCLI;

public class EntraTests : TopazFixture
{
	[Test]
	public async Task User_Create_With_AzureCli_Creates_User()
	{
		var upn = $"topaz-{Guid.NewGuid():N}@mytenant.onmicrosoft.com";
		string createdId = null!;

		await RunAzureCliCommand(
			$"az ad user create --display-name \"Topaz Test User\" --user-principal-name \"{upn}\" --password \"P@ssw0rd!\" --force-change-password-next-sign-in false -o json",
			(resp) =>
			{
				Assert.That(resp["id"], Is.Not.Null);
				createdId = resp["id"]!.GetValue<string>();
				Assert.That(resp["userPrincipalName"]!.GetValue<string>(), Is.EqualTo(upn));
			});

		if (!string.IsNullOrEmpty(createdId))
		{
			await RunAzureCliCommand($"az ad user delete --id {createdId}");
		}
	}
	
	[Test]
	public async Task User_Created_With_AzureCli_Can_Login_With_Username_And_Password()
	{
		var upn = $"topaz-{Guid.NewGuid():N}@mytenant.onmicrosoft.com";
		const string password = "P@ssw0rd!";
		string createdId = null!;

		try
		{
			await RunAzureCliCommand(
				$"az ad user create --display-name \"Topaz Test User\" --user-principal-name \"{upn}\" --password \"{password}\" --force-change-password-next-sign-in false -o json",
				(resp) =>
				{
					Assert.That(resp["id"], Is.Not.Null);
					createdId = resp["id"]!.GetValue<string>();
					Assert.That(resp["userPrincipalName"]!.GetValue<string>(), Is.EqualTo(upn));
				});

			// Ensure we are not accidentally using the admin session from OneTimeSetUp.
			await RunAzureCliCommand("az logout");

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
			await RunAzureCliCommand("az logout");
			await RunAzureCliCommand("az login --username topazadmin@topaz.local --password admin");

			if (!string.IsNullOrEmpty(createdId))
			{
				await RunAzureCliCommand($"az ad user delete --id {createdId}");
			}
		}
	}
	
	[Test]
	public async Task ServicePrincipal_Created_In_Emulated_Entra_Can_Login_With_AzureCli()
	{
		var spName = $"topaz-sp-{Guid.NewGuid():N}";
		string appId = null!;
		string password = null!;
		string tenant = null!;

		try
		{
			// Creates an application + service principal + client secret in the (emulated) tenant.
			// --skip-assignment keeps this test focused on auth, not RBAC.
			await RunAzureCliCommand(
				$"az ad sp create-for-rbac --name \"{spName}\" --skip-assignment --years 1 -o json",
				(resp) =>
				{
					Assert.That(resp["appId"], Is.Not.Null);
					Assert.That(resp["password"], Is.Not.Null);
					Assert.That(resp["tenant"], Is.Not.Null);

					appId = resp["appId"]!.GetValue<string>();
					password = resp["password"]!.GetValue<string>();
					tenant = resp["tenant"]!.GetValue<string>();
				});

			// Ensure we are not accidentally using the admin session from OneTimeSetUp.
			await RunAzureCliCommand("az logout");

			// Login using the created service principal credentials.
			await RunAzureCliCommand(
				$"az login --service-principal --username \"{appId}\" --password \"{password}\" --tenant \"{tenant}\" --allow-no-subscriptions -o json");

			// Verify the signed-in identity is a service principal and matches the appId.
			await RunAzureCliCommand(
				"az account show -o json",
				(resp) =>
				{
					Assert.That(resp["user"], Is.Not.Null);
					Assert.That(resp["user"]!["type"]!.GetValue<string>(), Is.EqualTo("servicePrincipal"));
					Assert.That(resp["user"]!["name"]!.GetValue<string>(), Is.EqualTo(appId));
				});
		}
		finally
		{
			// Restore admin login for any subsequent tests in this fixture.
			await RunAzureCliCommand("az logout");
			await RunAzureCliCommand("az login --username topazadmin@topaz.local --password admin");

			// Cleanup the created application (also removes the linked service principal).
			if (!string.IsNullOrWhiteSpace(appId))
			{
				await RunAzureCliCommand($"az ad app delete --id \"{appId}\"");
			}
		}
	}
}