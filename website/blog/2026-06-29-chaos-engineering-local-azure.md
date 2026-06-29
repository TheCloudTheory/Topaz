---
slug: chaos-engineering-local-azure-fault-injection
title: "Testing Azure retry logic locally: why I stopped mocking 429s and started injecting them"
description: Mocking HttpClient to return 429 or 503 tests your mock setup, not the SDK's retry pipeline. Topaz's fault injection engine sits between the auth check and the endpoint handler, so the full Azure SDK stack runs under real fault conditions on localhost.
keywords: [azure chaos engineering local, fault injection azure emulator, azure retry logic testing, topaz chaos, azure 429 local test, azure resilience testing local, azure throttling test]
authors: kamilmrzyglod
tags: [general, testing, devops]
---

There is a certain category of test that feels good to write but does not actually test what you think it does. Retry logic sits squarely in that category.

The usual pattern is this: inject a fake `HttpMessageHandler`, make it return a 429 or 503 on the first N calls, assert that the code retried and eventually succeeded. The test passes. You ship with confidence. Then, in production, a real throttling event triggers a path through the Azure SDK that your mock never covered, and the retry policy does not behave the way the test implied.

The issue is not that the mock is wrong. It is that the mock bypasses the entire SDK transport layer. When you return a 429 from a fake handler, you are testing whether your own retry wrapper handles it correctly. You are not testing whether `Azure.Core`'s built-in retry pipeline fires, whether the `Retry-After` header is respected, or whether the SDK's own exception hierarchy propagates through your application code the way you assumed. That is a different bar entirely.

{/* truncate */}

:::note[Coming in v1.8]
Fault injection is available in the nightly build today and will ship as a stable feature in Topaz v1.8. All commands below work against a nightly `topaz-host` instance.

```bash
docker pull thecloudtheory/topaz-host:nightly
```

[Chaos engineering docs →](https://topaz.thecloudtheory.com/docs/chaos-engineering/) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## Where the real SDK retry pipeline lives

The Azure SDK for .NET (and its equivalents in Python, Java, and Go) runs every outgoing request through a pipeline of policies. `RetryPolicy` sits in that pipeline. When a response comes back with a 429, `RetryPolicy` checks the `Retry-After` header, waits the specified duration, and retries. It does this transparently, below the level of the code that called `GetSecretAsync` or `SendMessageAsync`.

For that pipeline to actually exercise your retry logic, the 429 has to arrive through it. A fake `HttpMessageHandler` intercepts the request before it reaches the pipeline's transport step. Some policies still run. Others do not, depending on exactly where you injected the handler. The end result is that your retry test may be exercising a different code path than the one that runs in production.

What you actually want is something that lets the full SDK stack run, including pipeline initialization, token acquisition, and the retry machinery, and then injects the fault at the protocol boundary, after all of that setup, but before the real endpoint handler responds.

## How Topaz injects faults

The fault injection engine in Topaz sits inside the request router, in this position:

```
Request
  → Authentication check
  → Provider registration check
  → Chaos fault roll       ← injected here
  → Endpoint handler
  → Response
```

By the time a fault fires, the SDK has already acquired a token, serialized the request, and gone through its full pipeline. The fault response comes back through the same transport path as a real Azure response. If your SDK is configured to respect `Retry-After` on 429s, it will find a `Retry-After: 5` header in the response and behave accordingly. If your retry wrapper catches `RequestFailedException`, it will be thrown the same way it would be thrown against real Azure.

There are two controls. A global on/off switch, which I called chaos mode because it has to be explicit, and individual fault rules that define what to inject, at what rate, and against which service namespace. Nothing fires unless chaos mode is enabled, so you cannot accidentally leave a throttle rule active and wonder why your tests are slow the next morning.

## Creating a fault rule

The `topaz` CLI manages everything. To verify that your Key Vault retry logic actually works:

```bash
topaz chaos enable

topaz chaos rule create \
  --rule-id kv-throttle \
  --namespace Microsoft.KeyVault \
  --fault-type Throttle \
  --rate 0.5
```

With this rule active, roughly half of all Key Vault requests will receive a `429 Too Many Requests` with a `Retry-After: 5` header. The other half go through normally. That is intentional. A `--rate 1.0` rule that throttles every request is useful for verifying that your retry policy eventually gives up correctly, but it is not a very interesting test. A `--rate 0.5` rule means some requests succeed without any retry, some succeed after one retry, and occasionally the SDK exhausts its retry budget on a bad run. That mirrors how throttling actually behaves in a loaded Azure environment.

The four fault types cover the failure modes that Azure SDKs are expected to handle:

| Fault type | What the SDK sees |
|---|---|
| `TransientError` | `500 Internal Server Error`, standard Azure error body |
| `Throttle` | `429 Too Many Requests` with `Retry-After: 5` |
| `Timeout` | `408 Request Timeout`, delayed 30 seconds |
| `ServiceUnavailable` | `503 Service Unavailable`, delayed 60 seconds |

The `Timeout` and `ServiceUnavailable` faults are the ones that expose a different class of bugs. They are not retry bugs. They are timeout bugs. An application that handles 429 correctly often has no timeout on its `GetSecretAsync` calls at all, because under normal conditions it never needs one. A `Timeout` fault at 30 seconds will reveal whether your cancellation token propagation is correct. A `ServiceUnavailable` fault at 60 seconds will reveal whether a hardcoded `HttpClient.Timeout` of 30 seconds quietly swallows the response before the SDK even sees it.

## What a test actually looks like

A realistic scenario: a Key Vault secret read, using the actual `Azure.Security.KeyVault.Secrets` SDK, with a throttle rule active at 50% rate. The test class owns its chaos lifecycle — enable on setup, disable and clean up on teardown.

```csharp
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;
using Topaz.SDK;

[TestFixture]
public class KeyVaultRetryTests
{
    private const string VaultName = "retry-test-vault";
    private const string RuleId = "kv-throttle-test";

    [OneTimeSetUp]
    public async Task CreateVault()
    {
        // Provision the vault through the ARM control plane — same as you would in real Azure.
        await Program.RunAsync([
            "keyvault", "create",
            "--name", VaultName,
            "--resource-group", "rg-local",
            "--location", "westeurope"
        ]);

        await Program.RunAsync([
            "keyvault", "secret", "set",
            "--vault-name", VaultName,
            "--name", "db-password",
            "--value", "s3cr3t"
        ]);
    }

    [SetUp]
    public async Task EnableChaos()
    {
        await Program.RunAsync(["chaos", "enable"]);
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "Microsoft.KeyVault",
            "--fault-type", "Throttle",
            "--rate", "0.5"
        ]);
    }

    [TearDown]
    public async Task DisableChaos()
    {
        try { await Program.RunAsync(["chaos", "rule", "delete", "--rule-id", RuleId]); } catch { }
        try { await Program.RunAsync(["chaos", "disable"]); } catch { }
    }

    [Test]
    public async Task GetSecret_WithThrottleRuleActive_EventuallySucceeds()
    {
        // SecretClient is the real Azure SDK client — no mocks, no fake handlers.
        // DisableChallengeResourceVerification is required because the local endpoint
        // does not return a standard Azure resource challenge on the WWW-Authenticate header.
        var client = new SecretClient(
            TopazResourceHelpers.GetKeyVaultEndpoint(VaultName),
            new AzureLocalCredential(Globals.GlobalAdminId),
            new SecretClientOptions { DisableChallengeResourceVerification = true });

        // Azure.Core's built-in RetryPolicy fires here.
        // With faultRate 0.5, roughly half the attempts receive a 429 with Retry-After: 5.
        // The SDK retries transparently; the call eventually returns the secret.
        var response = await client.GetSecretAsync("db-password");

        Assert.That(response.Value.Value, Is.EqualTo("s3cr3t"));
    }
}
```

The test asserts that the secret is eventually returned correctly. If the retry policy is wired up, it passes. If `Retry-After` is being ignored, or the SDK is not retrying the exception type that 429 produces, it fails with a `RequestFailedException` instead. That is the thing the mock-based version never caught: it was asserting that your retry wrapper returned the right value, not that `Azure.Core`'s policy fired at all.

Two things worth noting about the setup. `DisableChallengeResourceVerification = true` is required on the `SecretClientOptions` when connecting to any non-Azure endpoint — without it, the SDK sends a bearer challenge to `vault.azure.net` and rejects the local token. `AzureLocalCredential` is from the Topaz SDK and issues real JWT tokens for the given principal OID; it works the same way as `DefaultAzureCredential` from the application's perspective, just without hitting an Entra tenant.

## Scoping rules to a service

Rules target Azure provider namespaces, so you can fault one service without affecting others running in the same Topaz instance:

```bash
# Throttle only Key Vault
topaz chaos rule create --rule-id kv-throttle --namespace Microsoft.KeyVault --fault-type Throttle --rate 0.3

# Separately: make Storage transiently fail
topaz chaos rule create --rule-id storage-transient --namespace Microsoft.Storage --fault-type TransientError --rate 0.2
```

Both rules are active independently. A request to Service Bus goes through without any fault. A request to Key Vault has a 30% chance of receiving a 429. A request to Storage has a 20% chance of receiving a 500.

There is one gap worth knowing about. Data-plane endpoints that do not carry a provider namespace (AMQP messaging, blob data-plane operations in some cases) are not reachable by namespace-scoped rules. To inject faults across all endpoints including those, use `--namespace '*'`. That is a broader hammer, but it works.

## Disabling rules between tests

Rules persist across test runs unless explicitly deleted. I find it cleaner to delete rules in a test teardown rather than toggling them:

```bash
topaz chaos rule delete --rule-id kv-throttle
topaz chaos rule delete --rule-id storage-transient
topaz chaos disable
```

Alternatively, individual rules can be disabled and re-enabled without deletion if you want to pause a rule mid-session:

```bash
topaz chaos rule disable --rule-id kv-throttle
# ... run tests that should not be faulted ...
topaz chaos rule enable --rule-id kv-throttle
```

## What this does not replace

The fault injection engine targets the control and data planes of emulated Azure services. It does not simulate network partition, clock skew, or certificate expiry. It also does not help with load-level testing: if you need to verify behavior under high concurrency with throttling, you need real throughput behind the requests, and a local emulator running on a developer machine is not the right tool for that.

What it does replace is the class of mock-based tests that return fault responses from a fake handler. If your retry logic depends on `Azure.Core`'s pipeline, those tests give you false confidence. Fault injection at the protocol boundary tests the thing you actually care about.

The full reference for chaos mode, fault types, and the REST API is in the [chaos engineering docs](https://topaz.thecloudtheory.com/docs/chaos-engineering/). The CLI reference covers every flag. If you find a fault type that would be useful and is not there yet, the issue tracker is open.
