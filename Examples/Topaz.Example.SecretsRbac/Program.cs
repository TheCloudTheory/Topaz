using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;
using Topaz.ResourceManager;

// ---------------------------------------------------------------------------
// Topaz.Example.SecretsRbac
//
// Demonstrates:
//   1. Creating a Key Vault and storing a secret
//   2. Creating a Managed Identity and assigning Key Vault Secrets User role
//   3. Reading the secret as the assigned identity (positive case)
//   4. Verifying access is denied for an unassigned identity (negative case)
//
// Prerequisites:
//   - topaz-host running with --default-subscription 00000000-0000-0000-0000-000000000001
//   - DNS setup and certificate trusted (see docs/intro.md)
// ---------------------------------------------------------------------------

const string subscriptionId = "00000000-0000-0000-0000-000000000001";
const string resourceGroupName = "rg-rbac-demo";
const string vaultName = "kv-rbac-demo";
const string secretName = "DatabasePassword";
const string secretValue = "super-secret-value";

// Key Vault Secrets User built-in role
const string kvSecretsUserRoleId = "4633458b-17de-408a-b874-0445c86b69e6";

// ---  ARM client authenticated as global admin  ---
var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
var armClient = new ArmClient(adminCredential, subscriptionId);

var subscription = await armClient.GetDefaultSubscriptionAsync();

// Step 1: Resource group
Console.WriteLine("Creating resource group...");
var rgOperation = await subscription.GetResourceGroups().CreateOrUpdateAsync(
    WaitUntil.Completed,
    resourceGroupName,
    new ResourceGroupData(Azure.Core.AzureLocation.WestEurope));
var rg = rgOperation.Value;

// Step 2: Key Vault (via CLI — Azure.ResourceManager.KeyVault control-plane is supported)
Console.WriteLine("Creating Key Vault and storing secret...");
RunAzCli($"keyvault create --name {vaultName} --resource-group {resourceGroupName} --location westeurope");
RunAzCli($"keyvault secret set --vault-name {vaultName} --name {secretName} --value \"{secretValue}\"");

// Step 3: Create a fake Managed Identity principal (Topaz accepts any GUID as a principal)
var assignedPrincipalId = Guid.NewGuid();
Console.WriteLine($"Managed Identity principal: {assignedPrincipalId}");

// Step 4: Assign Key Vault Secrets User role scoped to the vault
Console.WriteLine("Creating role assignment...");
var kvScope = new ResourceIdentifier(
    $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}");

await armClient
    .GetRoleAssignments(kvScope)
    .CreateOrUpdateAsync(
        WaitUntil.Completed,
        Guid.NewGuid().ToString(),
        new RoleAssignmentCreateOrUpdateContent(
            new ResourceIdentifier($"/providers/Microsoft.Authorization/roleDefinitions/{kvSecretsUserRoleId}"),
            assignedPrincipalId)
        {
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal
        });

// Step 5: Read secret as the assigned identity
Console.WriteLine("\n--- Positive test: assigned identity reads secret ---");
var assignedCredential = new AzureLocalCredential(assignedPrincipalId.ToString());
var vaultUri = TopazResourceHelpers.GetKeyVaultEndpoint(vaultName);
var secretClient = new SecretClient(vaultUri, assignedCredential);

var secret = await secretClient.GetSecretAsync(secretName);
Console.WriteLine($"Secret value: {secret.Value.Value}");
Console.WriteLine("✅ Access granted as expected.");

// Step 6: Attempt access with an unassigned identity — should be denied
Console.WriteLine("\n--- Negative test: unassigned identity is denied ---");
var unassignedCredential = new AzureLocalCredential(Guid.NewGuid().ToString());
var unauthorisedClient = new SecretClient(vaultUri, unassignedCredential);

try
{
    await unauthorisedClient.GetSecretAsync(secretName);
    Console.WriteLine("❌ ERROR: access should have been denied.");
}
catch (RequestFailedException ex) when (ex.Status == 403)
{
    Console.WriteLine($"✅ Access denied with 403 as expected: {ex.Message}");
}

static void RunAzCli(string arguments)
{
    var process = new System.Diagnostics.Process
    {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "az",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }
    };
    process.Start();
    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new InvalidOperationException(
            $"az {arguments} failed: {process.StandardError.ReadToEnd()}");
}
