namespace Topaz.Tests.AzureCLI;

/// <summary>
/// CLI-level tests for the Router-level chaos fault injection middleware.
/// Positive injection cases (TransientError / Throttle with faultRate≈1) are covered
/// by the in-process E2E tests in Topaz.Tests. Here we focus on the "no fault" paths
/// that have clean teardown behaviour: zero fault rate, non-matching serviceNamespace,
/// and chaos globally disabled.
/// </summary>
public class ChaosInjectionTests : TopazFixture
{
    private string _ruleId = string.Empty;

    private const string ChaosBase = "https://topaz.local.dev:8899/topaz/chaos";
    private const string RulesBase = "https://topaz.local.dev:8899/topaz/chaos/rules";

    // Non-Topaz ARM endpoint used as chaos injection target (authenticated by az cli).
    private const string TargetUri = "https://topaz.local.dev:8899/subscriptions";

    // Topaz-namespaced endpoint — always exempt from chaos injection.
    private const string TopazTargetUri = RulesBase;

    [SetUp]
    public async Task SetUp()
    {
        _ruleId = $"cli-inj-{Guid.NewGuid():N}";
        await RunAzureCliCommand($"az rest --method post --uri \"{ChaosBase}/enable\"");
    }

    [TearDown]
    public async Task TearDown()
    {
        // Disable chaos first (best-effort), then remove the rule.
        // Both calls may be faulted if a high-rate rule is still active; wrap each in
        // try/catch so a failed disable attempt does not prevent rule cleanup.
        try { await RunAzureCliCommand($"az rest --method post --uri \"{ChaosBase}/disable\""); } catch { }
        try { await RunAzureCliCommand($"az rest --method delete --uri \"{RulesBase}/{_ruleId}\""); } catch { }
        // Second pass: if the first disable was faulted, retry once the rule may be gone.
        try { await RunAzureCliCommand($"az rest --method post --uri \"{ChaosBase}/disable\""); } catch { }
    }

    // -------------------------------------------------------------------------
    // faultRate = 0.0 — rule can never fire
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ZeroFaultRate_NeverFires()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"{RulesBase}/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"TransientError\",\"faultRate\":0.0}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        for (var i = 0; i < 3; i++)
        {
            await RunAzureCliCommand(
                $"az rest --method get --uri \"{TargetUri}\"",
                response => Assert.That(response["value"], Is.Not.Null.Or.Empty),
                0);
        }
    }

    // -------------------------------------------------------------------------
    // serviceNamespace filtering — non-matching namespace must not inject a fault
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ServiceNamespace_NonMatchingNamespace_DoesNotFire()
    {
        // Scope the rule to Microsoft.Compute — the target URL has no Compute path segment.
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"{RulesBase}/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"Microsoft.Compute\",\"faultType\":\"TransientError\",\"faultRate\":0.9999}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method get --uri \"{TargetUri}\"",
            response => Assert.That(response["value"], Is.Not.Null.Or.Empty),
            0);
    }

    // -------------------------------------------------------------------------
    // Chaos globally disabled — rule exists but engine is off
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ChaosDisabled_RequestSucceeds()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"{RulesBase}/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"TransientError\",\"faultRate\":0.9999}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        // Disable chaos before making the target call — the disable call itself is
        // clean at this point because the rule was just created and chaos was enabled
        // as part of SetUp with no prior faulted requests in this sequence.
        await RunAzureCliCommand($"az rest --method post --uri \"{ChaosBase}/disable\"");

        await RunAzureCliCommand(
            $"az rest --method get --uri \"{TargetUri}\"",
            response => Assert.That(response["value"], Is.Not.Null.Or.Empty),
            0);
    }

    // -------------------------------------------------------------------------
    // Topaz endpoints are exempt — chaos never fires on /topaz/* paths
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_TopazEndpointsAreExempt_FromChaosInjection()
    {
        // Wildcard rule with rate ≈ 1 — fires on any non-Topaz endpoint.
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"{RulesBase}/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"TransientError\",\"faultRate\":0.9999}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        // Topaz-namespaced endpoint must return 200 despite the active rule.
        await RunAzureCliCommand(
            $"az rest --method get --uri \"{TopazTargetUri}\"",
            response => Assert.That(response["value"], Is.Not.Null.Or.Empty),
            0);
    }
}
