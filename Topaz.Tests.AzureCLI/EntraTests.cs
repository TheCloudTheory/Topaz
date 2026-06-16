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
			// || true: az logout exits 1 when no account is active (e.g. if the login under test
			// failed), which would abort the finally block before the restore login runs.
			await RunAzureCliCommand("az logout || true");
			await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");

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
			// Single-quote the password so /bin/sh doesn't expand $ or other shell-special
			// characters that Topaz's password generator includes.
			var spPasswordShellSafe = "'" + password.Replace("'", "'\\''") + "'";
			await RunAzureCliCommand(
				$"az login --service-principal --username \"{appId}\" --password {spPasswordShellSafe} --tenant \"{tenant}\" --allow-no-subscriptions -o json");

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
			// || true: az logout exits 1 when no account is active (e.g. if the SP login failed),
			// which would abort the finally block before the restore login runs.
			await RunAzureCliCommand("az logout || true");
			await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");

			// Cleanup the created application (also removes the linked service principal).
			if (!string.IsNullOrWhiteSpace(appId))
			{
				await RunAzureCliCommand($"az ad app delete --id \"{appId}\"");
			}
		}
	}
	
	[Test]
	public async Task User_Cannot_Login_With_Invalid_Username_Or_Password()
	{
		var upn = $"topaz-{Guid.NewGuid():N}@mytenant.onmicrosoft.com";
		const string correctPassword = "P@ssw0rd!";
		const string wrongPassword = "WrongPassword!123";
		string createdId = null!;

		try
		{
			await RunAzureCliCommand(
				$"az ad user create --display-name \"Topaz Test User\" --user-principal-name \"{upn}\" --password \"{correctPassword}\" --force-change-password-next-sign-in false -o json",
				(resp) =>
				{
					Assert.That(resp["id"], Is.Not.Null);
					createdId = resp["id"]!.GetValue<string>();
					Assert.That(resp["userPrincipalName"]!.GetValue<string>(), Is.EqualTo(upn));
				});

			// Ensure we are not accidentally using the admin session from OneTimeSetUp.
			await RunAzureCliCommand("az logout");

			// Wrong password should not authenticate.
			await RunAzureCliCommand(
				$"az login --username \"{upn}\" --password \"{wrongPassword}\" --allow-no-subscriptions",
				assertion: null,
				exitCode: 1);

			// Wrong username should not authenticate.
			var invalidUpn = $"does-not-exist-{Guid.NewGuid():N}@mytenant.onmicrosoft.com";
			await RunAzureCliCommand(
				$"az login --username \"{invalidUpn}\" --password \"{correctPassword}\" --allow-no-subscriptions",
				assertion: null,
				exitCode: 1);
		}
		finally
		{
			// Restore admin login for any subsequent tests in this fixture.
			await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");

			if (!string.IsNullOrEmpty(createdId))
			{
				await RunAzureCliCommand($"az ad user delete --id {createdId}");
			}
		}
	}

	[Test]
	public async Task DeviceCode_Post_And_TokenExchange_ReturnsAccessToken()
	{
		// az login --use-device-code is inherently interactive and cannot be automated.
		// Drive the underlying HTTP flow with curl: POST to the device code endpoint, capture
		// device_code and user_code, simulate browser sign-in at /devicelogin, then exchange
		// for an access token.
		var command =
			"RESPONSE=$(curl -sf --cacert /tmp/topaz.crt " +
				"-X POST https://topaz.local.dev:8899/organizations/oauth2/v2.0/devicecode " +
				"-H 'Content-Type: application/x-www-form-urlencoded' " +
				"-d 'client_id=00000000-0000-0000-0000-000000000001&scope=https%3A%2F%2Fmanagement.azure.com%2F.default') " +
			"&& DEVICE_CODE=$(echo \"$RESPONSE\" | python3 -c 'import sys,json;print(json.load(sys.stdin)[\"device_code\"])') " +
			"&& USER_CODE=$(echo \"$RESPONSE\" | python3 -c 'import sys,json;print(json.load(sys.stdin)[\"user_code\"])') " +
			"&& curl -sf --cacert /tmp/topaz.crt " +
				"-X POST https://topaz.local.dev:8899/devicelogin " +
				"-H 'Content-Type: application/x-www-form-urlencoded' " +
				"--data-urlencode \"user_code=${USER_CODE}\" " +
				"--data-urlencode 'username=topazadmin@topaz.local.dev' " +
				"> /dev/null " +
			"&& curl -sf --cacert /tmp/topaz.crt " +
				"-X POST https://topaz.local.dev:8899/organizations/oauth2/v2.0/token " +
				"-H 'Content-Type: application/x-www-form-urlencoded' " +
				"--data-urlencode 'grant_type=urn:ietf:params:oauth:grant-type:device_code' " +
				"--data-urlencode \"device_code=${DEVICE_CODE}\" " +
				"--data-urlencode 'client_id=00000000-0000-0000-0000-000000000001'";

		await RunAzureCliCommand(command, resp =>
		{
			Assert.That(resp["access_token"], Is.Not.Null);
			Assert.That(resp["access_token"]!.GetValue<string>(), Is.Not.Empty);
		});
	}
}