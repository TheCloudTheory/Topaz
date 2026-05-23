namespace Topaz.Tests.Python;

/// <summary>
/// One NUnit [Test] method per Python test file.  Each test registers the
/// required hostnames in the Python container and then delegates to pytest.
///
/// Naming follows the convention used by Topaz.Tests.AzureCLI:
/// the test method name describes the service / scenario being tested.
/// </summary>
public class PythonTestRunner
{
    [Test]
    public async Task Python_KeyVaultTests()
    {
        await PythonHostMapper.EnsureKeyVaultHostsMapped("pytest-kv");
        await PythonFixture.RunPythonTests("test_key_vault.py");
    }

    [Test]
    public async Task Python_ServiceBusTests()
    {
        await PythonHostMapper.EnsureServiceBusHostsMapped("py-sb-test");
        await PythonFixture.RunPythonTests("test_service_bus.py");
    }

    [Test]
    public async Task Python_StorageAccountTests()
    {
        await PythonFixture.RunPythonTests("test_storage_account.py");
    }

    [Test]
    public async Task Python_BlobStorageTests()
    {
        await PythonHostMapper.EnsureStorageHostsMapped("pyblobstoretest");
        await PythonFixture.RunPythonTests("test_blob_storage.py");
    }

    [Test]
    public async Task Python_TableStorageTests()
    {
        await PythonHostMapper.EnsureStorageHostsMapped("pytablestortest", "pytblragrstest");
        await PythonFixture.RunPythonTests("test_table_storage.py");
    }

    [Test]
    public async Task Python_QueueStorageTests()
    {
        await PythonHostMapper.EnsureStorageHostsMapped("pyqueuestortest");
        await PythonFixture.RunPythonTests("test_queue_storage.py");
    }

    [Test]
    public async Task Python_ContainerRegistryTests()
    {
        await PythonHostMapper.EnsureContainerRegistryHostsMapped(
            "pyacrtest01", "pyacrmgid01",
            "pyacrempty", "pyacrpush",
            "pyacrtags01", "pyacrpaginate",
            "pyacrdelmfst", "pyacrdeldgst", "pyacrdelnotfnd",
            "pyacrhead01", "pyacrheaddig", "pyacrheadnf",
            "pyacrblob01"
        );
        await PythonFixture.RunPythonTests("test_acr.py");
    }

    [Test]
    public async Task Python_AuthorizationTests()
    {
        await PythonFixture.RunPythonTests("test_authorization.py");
    }

    [Test]
    public async Task Python_EventHubTests()
    {
        await PythonFixture.RunPythonTests("test_event_hub.py");
    }

    [Test]
    public async Task Python_ManagedIdentityTests()
    {
        await PythonFixture.RunPythonTests("test_managed_identity.py");
    }

    [Test]
    public async Task Python_VirtualNetworkTests()
    {
        await PythonFixture.RunPythonTests("test_virtual_network.py");
    }

    [Test]
    public async Task Python_VirtualMachineTests()
    {
        await PythonFixture.RunPythonTests("test_virtual_machine.py");
    }

    [Test]
    public async Task Python_SubscriptionTests()
    {
        await PythonFixture.RunPythonTests("test_subscription.py");
    }

    [Test]
    public async Task Python_AppServiceTests()
    {
        await PythonFixture.RunPythonTests("test_app_service.py");
    }

    [Test]
    public async Task Python_ManagementGroupTests()
    {
        await PythonFixture.RunPythonTests("test_management_group.py");
    }
}
