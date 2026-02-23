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
}