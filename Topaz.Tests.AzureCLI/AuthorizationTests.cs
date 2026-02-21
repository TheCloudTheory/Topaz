using System.Text.Json.Nodes;

namespace Topaz.Tests.AzureCLI;

public class AuthorizationTests : TopazFixture
{
	[Test]
	public async Task RoleAssignment_Create_List_Update_ListChangelogs_And_Delete()
	{
		var clientId = Guid.NewGuid().ToString();
		string subscription = null!;
		string assignmentId = null!;

		await RunAzureCliCommand("az account show", (resp) => { subscription = resp["id"]!.GetValue<string>(); });

		// create
		await RunAzureCliCommand($"az role assignment create --assignee {clientId} --role \"Contributor\" --scope {subscription}", (resp) =>
		{
			Assert.That(resp["id"], Is.Not.Null);
			assignmentId = resp["id"]!.GetValue<string>();
		});

		// list
		await RunAzureCliCommand($"az role assignment list --assignee {clientId}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["id"]!.GetValue<string>() == assignmentId), Is.True);
		});

		// update (change role to Reader) - construct the role-assignment JSON from known values and update via --role-assignment
		var subscriptionScope = subscription.StartsWith("/subscriptions/") ? subscription : $"/subscriptions/{subscription}";
		var readerRoleDefId = $"{subscriptionScope}/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";
		var name = assignmentId.Split('/').Last();
		var updated = new JsonObject
		{
			["id"] = assignmentId,
			["name"] = name,
			["type"] = "Microsoft.Authorization/roleAssignments",
			["principalId"] = clientId,
			["principalType"] = "ServicePrincipal",
			["roleDefinitionId"] = readerRoleDefId,
			["scope"] = subscriptionScope
		};

		var updatedJson = updated.ToJsonString();
		await RunAzureCliCommand($"az role assignment update --role-assignment '{updatedJson}'");

		await RunAzureCliCommand($"az role assignment list --assignee {clientId}", (resp) =>
		{
			var arr = resp.AsArray();
			var item = arr.FirstOrDefault(a => a!["id"]!.GetValue<string>() == assignmentId)!;
			Assert.That(item, Is.Not.Null);
			Assert.That(item["roleDefinitionId"]!.GetValue<string>(), Is.EqualTo(readerRoleDefId));
		});

		// list changelogs (call without --assignee to avoid unsupported argument in container)
		await RunAzureCliCommand("az role assignment list-changelogs", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr, Is.Not.Null);
		});

		// delete
		await RunAzureCliCommand($"az role assignment delete --ids {assignmentId}");

		await RunAzureCliCommand($"az role assignment list --assignee {clientId}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["id"]!.GetValue<string>() == assignmentId), Is.False);
		});
	}

	[Test]
	public async Task RoleDefinition_Create_Show_List_Update_And_Delete()
	{
		string subscription = null!;
		await RunAzureCliCommand("az account show", (resp) => { subscription = resp["id"]!.GetValue<string>(); });
		
		var subscriptionScope = subscription.StartsWith("/subscriptions/") ? subscription : $"/subscriptions/{subscription}";
		var roleName = $"TopazCustomRole{Guid.NewGuid():N}";
		var roleDef = new
		{
			Name = roleName,
			IsCustom = true,
			Description = "Topaz test custom role",
			Actions = new[] { "*/read" },
			AssignableScopes = new[] { $"/subscriptions/{subscription}" }
		};

		var json = System.Text.Json.JsonSerializer.Serialize(roleDef);

		// create
		await RunAzureCliCommand($"az role definition create --role-definition '{json}'", (resp) =>
		{
			Assert.That(resp["roleName"]!.GetValue<string>(), Is.EqualTo(roleName));
		});

		// list
		await RunAzureCliCommand($"az role definition list --name {roleName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.Any(a => a!["roleName"]!.GetValue<string>() == roleName), Is.True);
		});

		// show
		await RunAzureCliCommand($"az role definition show --name {roleName} --scope {subscriptionScope}", (resp) => {
            Assert.Multiple(() =>
		    {
		        Assert.That(resp["roleName"]!.GetValue<string>(), Is.EqualTo(roleName));
		    });
        });

		// update (change description)
		var updated = new { Name = roleName, IsCustom = true, Description = "Updated description", Actions = new[] { "*/read" }, AssignableScopes = new[] { $"/subscriptions/{subscription}" } };
		var updatedJson = System.Text.Json.JsonSerializer.Serialize(updated);
		await RunAzureCliCommand($"az role definition update --role-definition '{updatedJson}'", (resp) => {
            Assert.Multiple(() =>
		    {
		        Assert.That(resp["roleName"]!.GetValue<string>(), Is.EqualTo(roleName));
		        Assert.That(resp["description"]!.GetValue<string>(), Is.EqualTo("Updated description"));
		    });
        });

		// delete
		await RunAzureCliCommand($"az role definition delete --name {roleName} --scope {subscriptionScope}");

		// ensure removed
		await RunAzureCliCommand($"az role definition list --name {roleName}", (resp) =>
		{
			var arr = resp.AsArray();
			Assert.That(arr.All(a => a!["roleName"]!.GetValue<string>() != roleName), Is.True);
		});
	}
}