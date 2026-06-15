---
slug: ci-integration-tests-without-azure-subscription
title: "Running Azure integration tests in CI without a subscription, credentials, or cloud costs"
description: A walkthrough of replacing real Azure dependencies in GitHub Actions with a local Topaz emulator. Covers the four pain points of cloud-dependent CI, the GitHub Actions workflow, and a real timing comparison.
keywords: [azure ci integration tests, github actions azure emulator, azure service principal ci, azure integration tests without subscription, topaz ci, local azure ci pipeline, azure ci credentials]
authors: kamilmrzyglod
tags: [general, ci, devops]
---

Every team that tests against real Azure services in CI eventually hits the same four problems. You need credentials for the pipeline. Those credentials need to be stored somewhere, rotated, and audited, and if they leak they affect a real environment. The tests themselves become flaky because Azure provisioning has variable latency and your Service Bus namespace occasionally takes three minutes to appear. And if you run on private agents, you need to add networking complexity on top of all of that.

These are not Azure-specific problems. They show up in any pipeline that depends on external cloud services. But the fix for them often is Azure-specific, and the usual answers (use a dedicated test subscription, use Managed Identity, sanitize your test fixtures) treat the symptoms without changing the fundamental structure of the problem.

I wanted to see whether running tests against a local Azure emulator in CI could close those gaps cleanly. The short version is: it can, and the job runs in 38 seconds.

{/* truncate */}

:::tip[Try it yourself]
The full workflow and Compose file from this post are in the Topaz repository under [`Examples/CI/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/CI). The reference workflow in `.github/workflows/topaz-ci.yml` runs on demand via `workflow_dispatch`.

```bash
brew tap thecloudtheory/topaz && brew install topaz   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[CI/CD integration docs →](https://topaz.thecloudtheory.com/docs/ecosystem/ci-cd) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## The four problems in more detail

The credential problem sounds manageable until you operate it at scale. You create a service principal, assign it permissions, store the client secret in CI secrets, and document the rotation schedule. Six months later someone asks why a test job failed with an authentication error at 2am and the answer is that the secret expired and nobody updated it. Managed Identity helps if you control the runner infrastructure, but most teams using GitHub-hosted runners do not.

The secret leakage risk is a distinct concern. A service principal with Contributor rights on a test subscription can do quite a lot of damage if it ends up in a workflow log, a forked PR, or a misconfigured environment variable. The blast radius is real even if the probability is low. On the other hand, the supply chain attack targeting GitHub Actions is more and more common. Any chance to lower the chances of leaking cloud credentials should be evaluated and utilized.

Flakiness from transient errors is underappreciated. Azure resource provisioning is eventually consistent and the timing varies. A Key Vault that usually takes thirty seconds to create occasionally takes ninety. A Service Bus namespace that provisions in two minutes on a Tuesday might take seven on a Friday afternoon in a busy region. When your test fixture polls for a terminal state, it either times out conservatively (slow CI) or aggressively (intermittent failures). Neither is good.

The private agent networking problem is narrower but worth naming. If your runners sit inside a corporate network with egress filtering, every Azure service call has to traverse firewall rules, proxy configurations, and DNS resolution that can behave differently from your development machine. A failing integration test on a private runner is hard to debug precisely because the environment is not easily reproducible.

## What the setup looks like

The core idea is to replace the real Azure dependency with a Topaz container that starts fresh at the beginning of each workflow run and is discarded at the end. No shared state between runs. No credentials beyond the emulator's built-in admin account. No network calls leaving the runner.

The workflow needs four things before the tests can run:

1. DNS configuration so that Azure SDK hostnames (like `kv-ci.vault.topaz.local.dev`) resolve to `127.0.0.1`. Topaz ships an install script that configures `dnsmasq` for this. On a GitHub-hosted runner, this takes about 12 seconds.
2. Certificate trust at the OS level, so HTTPS calls from the SDK and CLI do not fail with certificate verify errors.
3. Certificate trust inside the Azure CLI's Python bundle, since the CLI maintains its own CA store and does not inherit the OS trust store.
4. The Topaz container itself, started with port 443 exposed (required for MSAL's user-realm pre-flight during `az login`) and `--default-subscription` set so the emulator creates a subscription automatically.

The Azure CLI then needs to be pointed at the Topaz cloud environment. Topaz ships a `cloud.json` that describes the custom endpoints and DNS suffixes. After registering it and logging in with the built-in credentials, the CLI behaves identically to a real Azure environment from the user's perspective.

```yaml
- name: Register Topaz cloud
  run: |
    az cloud register -n Topaz --cloud-config @cloud.json
    az cloud set -n Topaz

- name: Log in to Topaz
  env:
    AZURE_CORE_INSTANCE_DISCOVERY: "false"
  run: |
    az login \
      --username topazadmin@topaz.local.dev \
      --password admin \
      --allow-no-subscriptions
```

`AZURE_CORE_INSTANCE_DISCOVERY=false` is required because the Topaz Entra ID endpoint is not in Microsoft's public instance discovery list. The credentials here are not secrets — they ship with every Topaz install and only work against the local emulator. There is nothing to rotate.

## The test steps

With the environment in place, the workflow provisions a resource group, a Key Vault, a storage account, and a Service Bus namespace with a queue, then runs three data-plane assertions.

The Key Vault test is a round-trip: write a secret, read it back, compare:

```bash
az keyvault secret set \
  --vault-name "kv-ci" \
  --name "db-password" \
  --value "correct-horse-battery-staple"

VALUE=$(az keyvault secret show \
  --vault-name "kv-ci" \
  --name "db-password" \
  --query "value" -o tsv)

[ "$VALUE" = "correct-horse-battery-staple" ] \
  && echo "Key Vault: PASS" \
  || (echo "Key Vault: FAIL — got '$VALUE'" && exit 1)
```

The Blob Storage test uploads a file, downloads it, and diffs the result. One wrinkle: the Azure CLI's automatic account-key lookup path does not work against Topaz yet (tracked in the backlog), so you need to retrieve the connection string explicitly and pass it to each storage command:

```bash
STORAGE_CONN=$(az storage account show-connection-string \
  --name "stci001" \
  --resource-group "rg-ci" \
  --query connectionString -o tsv)

az storage blob upload \
  --container-name "artifacts" \
  --name "artifact.txt" \
  --file /tmp/artifact.txt \
  --connection-string "$STORAGE_CONN"
```

The Service Bus test stays at the ARM level — verifying that the queue was created and is reachable — rather than sending AMQP messages. `az servicebus` does not have a send/receive command; data-plane message operations require the SDK. For a CI smoke test, confirming the namespace and queue exist is sufficient.

## The timing

I ran the workflow on a GitHub-hosted `ubuntu-latest` runner. These are the step times:

| Step | Time |
|---|---|
| Configure DNS (`install-linux.sh`) | 12s |
| Install certificate (OS + Azure CLI) | 3s |
| Start Topaz | 5s |
| Wait for Topaz ready | 2s |
| Register cloud + log in | 4s |
| Provision resource group, Key Vault, storage account, Service Bus namespace + queue | 7s |
| Test Key Vault (secret write + read) | 1s |
| Test Blob Storage (upload + download + diff) | 2s |
| Test Service Bus (queue list) | 1s |
| Teardown | 0s |
| **Total** | **~38s** |

The DNS setup step at 12 seconds is the single largest cost, and it is a one-time runner setup. The provisioning step at 7 seconds covers five resources across three services. That same step on real Azure would spend most of its time on the Service Bus namespace alone, which async-polls until the broker cluster is ready and typically takes 60 to 120 seconds. On a busy day in a congested region, I have seen it take longer.

The more meaningful comparison is not average time but variance. The Topaz job is bounded: it takes roughly the same time every run because nothing depends on external provisioning. A real Azure pipeline is unbounded: your p99 is not two minutes, it is "until a throttled namespace eventually appears or the polling timeout fires." The tail latency is what breaks CI SLAs in practice, not the mean.

## What this does not solve

The approach works for integration tests that verify behavior against Azure data-plane APIs. It does not help with tests that need to verify Azure-specific behavior that Topaz does not emulate yet (CosmosDB data-plane queries, for instance, are still in progress). It is also not a replacement for a staging environment: if your application deploys to Azure and you need to test the deployment itself, you still need real infrastructure.

The credential problem is solved entirely. The flakiness problem is solved for provisioning latency; tests can still be flaky for application-level reasons, which is where they should be. The private agent networking problem is solved because there are no external calls. The cost problem, which I did not list above but should have, is solved: the only cloud resource consumed is the compute time for the runner itself.

One rough edge worth noting: the Azure CLI's implicit credential resolution for storage data-plane commands does not work against Topaz. When you run `az storage container create --account-name stci001` without explicit credentials, the CLI makes an internal ARM call to look up the account key automatically. That specific call returns 404 from Topaz. The fix is to retrieve the connection string explicitly via `az storage account show-connection-string` (which uses the `listKeys` endpoint that Topaz does implement) immediately after account creation, then pass it as `--connection-string` to every data-plane command. It is a small amount of extra plumbing, but it is predictable. The implicit lookup path will be fixed in v1.8.

## The practical takeaway

The test steps in this post use plain `az` commands, but that is not the point. Replace them with `dotnet test`, `pytest`, or a shell script and the structure stays the same. The setup steps (DNS, certificates, container start) are runner-level concerns that happen before your tests run. Your test code does not know or care that it is talking to an emulator rather than real Azure.

If you want to try this in your own pipeline, the workflow is in the Topaz repository at [`.github/workflows/topaz-ci.yml`](https://github.com/TheCloudTheory/Topaz/blob/main/.github/workflows/topaz-ci.yml). It runs on demand via `workflow_dispatch`, so you can trigger it against any branch without touching your main CI. The Azure CLI job is the `azure-cli-integration` step — the full sequence from DNS setup to teardown is there and annotated.
