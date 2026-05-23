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
}
