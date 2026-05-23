namespace Topaz.Tests.Python;

/// <summary>
/// Registers hostnames required by Python tests into the Python container's
/// /etc/hosts, mirroring the KeyVaultHostMapper / StorageHostMapper pattern
/// from Topaz.Tests.AzureCLI.
/// </summary>
internal static class PythonHostMapper
{
    /// <summary>
    /// Ensures all Key Vault subdomains for the given vault names are mapped
    /// inside the Python container.
    /// </summary>
    public static async Task EnsureKeyVaultHostsMapped(params string[] vaultNames)
    {
        foreach (var vaultName in vaultNames)
        {
            var lower = vaultName.ToLowerInvariant();
            await PythonFixture.EnsureHostMapping($"{lower}.vault.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}.keyvault.topaz.local.dev");
        }
    }

    /// <summary>
    /// Ensures all Storage Account subdomains (blob, queue, table) for the given
    /// account names are mapped inside the Python container.
    /// </summary>
    public static async Task EnsureStorageHostsMapped(params string[] accountNames)
    {
        foreach (var accountName in accountNames)
        {
            var lower = accountName.ToLowerInvariant();
            await PythonFixture.EnsureHostMapping($"{lower}.blob.storage.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}.queue.storage.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}.table.storage.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}-secondary.blob.storage.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}-secondary.queue.storage.topaz.local.dev");
            await PythonFixture.EnsureHostMapping($"{lower}-secondary.table.storage.topaz.local.dev");
        }
    }

    /// <summary>
    /// Ensures the Service Bus namespace subdomain is mapped inside the
    /// Python container.
    /// </summary>
    public static async Task EnsureServiceBusHostsMapped(params string[] namespaceNames)
    {
        foreach (var namespaceName in namespaceNames)
        {
            await PythonFixture.EnsureHostMapping(
                $"{namespaceName.ToLowerInvariant()}.servicebus.topaz.local.dev");
        }
    }
}
