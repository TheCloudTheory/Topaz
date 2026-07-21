namespace Topaz.Tests.Legacy.Java;

/// <summary>
/// Registers storage account hostnames in the Java container's /etc/hosts,
/// mirroring the pattern from Topaz.Tests.Python.PythonHostMapper.
/// </summary>
internal static class JavaHostMapper
{
    public static async Task EnsureStorageHostsMapped(params string[] accountNames)
    {
        foreach (var accountName in accountNames)
        {
            var lower = accountName.ToLowerInvariant();
            await JavaFixture.EnsureHostMapping($"{lower}.blob.storage.topaz.local.dev");
        }
    }
}
