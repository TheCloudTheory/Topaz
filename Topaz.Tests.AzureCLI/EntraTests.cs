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
}