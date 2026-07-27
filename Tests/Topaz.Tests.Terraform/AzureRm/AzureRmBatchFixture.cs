using System.Text.Json.Nodes;

namespace Topaz.Tests.Terraform.AzureRm;

/// <summary>
/// Base class for all AzureRM Terraform tests.
/// Applies the combined azurerm workspace ONCE at process start and destroys it at process exit,
/// reducing azurerm v4 provider startups from 34 (17 × apply+destroy) to 2.
/// Individual [Test] methods simply assert against the pre-captured outputs — no Terraform I/O.
/// </summary>
public class AzureRmBatchFixture : TopazFixture
{
    private static readonly SemaphoreSlim BatchLock = new(1, 1);
    private static bool _applied;
    private static Exception? _applyFailure;
    private static JsonNode? _outputs;
    private static string? _workDir;

    // Register destroy to run BEFORE containers are stopped (via the BeforeContainerCleanup hook).
    static AzureRmBatchFixture()
    {
        BeforeContainerCleanup += static () =>
        {
            var wd = Volatile.Read(ref _workDir);
            if (wd is not null && _applied)
                DestroyTerraformWorkspaceSync(wd);
        };
    }

    [OneTimeSetUp]
    public async Task OneTimeApplyAzureRmBatch()
    {
        await BatchLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_applied)
                return;

            // A previous fixture class already attempted the apply and it failed.
            // Re-applying with a fresh workspace would encounter Topaz resources already
            // created during the first attempt, producing misleading "resource already exists"
            // errors for every resource group instead of the original error.
            if (_applyFailure is not null)
            {
                Assert.Ignore(
                    "AzureRM batch workspace apply failed in an earlier fixture class — " +
                    $"skipping to avoid cascading re-apply errors.\n{_applyFailure.Message}");
                return;
            }

            try
            {
                (var outputs, var workDir) = await ApplyTerraformRetaining(
                    "providers/azurerm.tf", "azurerm-combined");

                Volatile.Write(ref _workDir, workDir);
                _outputs = outputs;
                _applied = true;
            }
            catch (Exception ex)
            {
                _applyFailure = ex;
                throw;
            }
        }
        finally
        {
            BatchLock.Release();
        }
    }

    [OneTimeTearDown]
    public Task OneTimeTearDownAzureRmBatch()
    {
        // Destroy is deferred to process exit via BeforeContainerCleanup so containers
        // are still running when terraform destroy executes.
        return Task.CompletedTask;
    }

    protected T GetOutput<T>(string key)
        => _outputs![key]!["value"]!.GetValue<T>();

    protected JsonNode GetOutputNode(string key)
        => _outputs![key]!["value"]!;
}
