namespace Topaz.Tests.NodeJS;

/// <summary>
/// Registers hostnames required by Node.js tests into the Node container's
/// /etc/hosts, mirroring the PythonHostMapper pattern.
/// </summary>
internal static class NodeJSHostMapper
{
    public static async Task EnsureServiceBusHostsMapped(params string[] namespaceNames)
    {
        foreach (var namespaceName in namespaceNames)
            await NodeJSFixture.EnsureHostMapping($"{namespaceName.ToLowerInvariant()}.servicebus.topaz.local.dev");
    }

    public static async Task EnsureEventHubHostsMapped(params string[] namespaceNames)
    {
        foreach (var namespaceName in namespaceNames)
            await NodeJSFixture.EnsureHostMapping($"{namespaceName.ToLowerInvariant()}.eventhub.topaz.local.dev");
    }
}
