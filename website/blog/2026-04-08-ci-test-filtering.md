---
slug: ci-test-filtering
title: "How Topaz CI runs only the tests that matter"
authors: kamilmrzyglod
tags: [general, cicd]
---

Running the full test suite on every commit is simple to set up and expensive to live with. Topaz spans twelve services, each with its own E2E tests and Azure CLI tests. On a change to a single endpoint in the Container Registry service, waiting for Key Vault, Service Bus, and Event Hubs tests to finish is pure overhead. The Topaz CI pipeline solves this with a three-stage decision that maps changed files to a focused test filter — running everything only when it has to.

<!-- truncate -->

## Stage one: skip CI entirely for non-code changes

The first filter sits at the workflow trigger level. The `push` event that starts a CI run explicitly ignores paths that cannot affect the emulated Azure services:

```yaml
on:
  push:
    branches: [ "main" ]
    paths-ignore:
      - 'README.md'
      - '.github/workflows/**'
      - 'Examples/**'
      - 'website/**'
      - 'static/**'
      - 'Topaz.MCP/**'
```

A documentation edit, a blog post, a change to a workflow definition, or an update to an example — none of these trigger the pipeline at all. The build job never starts. This matters most for the website, which receives frequent small updates. Without this filter, every docs commit would queue a full build-and-test run against the host process.

Pull requests always run regardless of path, which is the right default: you want feedback on every proposed change, but you do not want to pay for docs-only pushes to `main` on a continuous basis.

## Stage two: classify changed files by service

Once the pipeline is running, the second filter uses [`dorny/paths-filter`](https://github.com/dorny/paths-filter) to classify the diff into a set of named boolean outputs — one for each service and one for the shared infrastructure:

```yaml
- name: Detect changed paths
  id: changes
  uses: dorny/paths-filter@v3
  with:
    filters: |
      code:
        - 'Topaz.Host/**'
        - 'Topaz.ResourceManager/**'
        - 'Topaz.Shared/**'
        - 'Services/**'
        - ...
      core:
        - 'Topaz.Host/**'
        - 'Topaz.ResourceManager/**'
        - 'Topaz.Shared/**'
        - 'Topaz.Identity/**'
        - 'Services/Topaz.Service.Shared/**'
      keyvault:
        - 'Services/Topaz.Service.KeyVault/**'
      acr:
        - 'Services/Topaz.Service.ContainerRegistry/**'
      storage:
        - 'Services/Topaz.Service.Storage/**'
      ...
```

The `code` filter is a union of every path that contains runnable code. The `core` filter covers shared infrastructure: the host composition layer, the Resource Manager, shared types, Identity, and the shared service utilities. Individual service filters each cover exactly one service directory.

After this step, the pipeline knows which named groups contain changes. The next step uses that knowledge to decide what to test.

## Stage three: build a vstest filter expression

The third step translates the path-filter outputs into a `--filter` expression for `dotnet test`. The logic runs as a shell script and handles four distinct situations:

**Manual trigger** — `workflow_dispatch` always runs everything. This is the escape hatch for when you need to verify the full suite regardless of what changed:

```bash
if [[ "${{ github.event_name }}" == "workflow_dispatch" ]]; then
  echo "filter=" >> "$GITHUB_OUTPUT"
  echo "skip=false" >> "$GITHUB_OUTPUT"
  exit 0
fi
```

**No code changed** — if the `code` filter is false, no runnable source changed (only docs or config within paths that were not excluded at the trigger level). Tests are skipped entirely:

```bash
if [[ "${{ steps.changes.outputs.code }}" != "true" ]]; then
  echo "skip=true" >> "$GITHUB_OUTPUT"
  exit 0
fi
```

**Core infrastructure changed** — if `Topaz.Host`, `Topaz.Shared`, `Topaz.Identity`, or `Topaz.Service.Shared` changed, every service is potentially affected. The filter is left empty, which means `dotnet test` runs every test in the solution:

```bash
if [[ "${{ steps.changes.outputs.core }}" == "true" ]]; then
  echo "filter=" >> "$GITHUB_OUTPUT"
  exit 0
fi
```

**Per-service changes only** — for everything else, the script builds an `OR`-joined vstest filter expression using `FullyQualifiedName~` substring matching:

```bash
PARTS=()
[[ "${{ steps.changes.outputs.keyvault }}" == "true" ]] && PARTS+=("FullyQualifiedName~KeyVault")
[[ "${{ steps.changes.outputs.acr }}"      == "true" ]] && PARTS+=("FullyQualifiedName~ContainerRegistry")
[[ "${{ steps.changes.outputs.storage }}"  == "true" ]] && PARTS+=("FullyQualifiedName~Storage")
...

JOINED=$(IFS='|'; echo "${PARTS[*]}")
echo "filter=$JOINED" >> "$GITHUB_OUTPUT"
```

If a change touches only the Container Registry and Storage services, the filter becomes `FullyQualifiedName~ContainerRegistry|FullyQualifiedName~Storage`. `dotnet test` applies this as a substring match against the fully qualified test name, which naturally includes the service name as part of the namespace. Tests for Key Vault, Service Bus, and every other service are not loaded.

There is one safety fallback: if code changed (the `code` filter is true) but no individual service filter matched — for example, a change to `Topaz.CLI` — the filter is left empty and the full suite runs. An unknown change is treated as a potentially cross-cutting one.

## Applying the filter

The test step receives the filter as a step output and passes it directly to `dotnet test`:

```yaml
- name: Test
  if: steps.test_filter.outputs.skip != 'true'
  run: |
    FILTER="${{ steps.test_filter.outputs.filter }}"
    if [[ -n "$FILTER" ]]; then
      dotnet test -m:1 --no-build --verbosity normal --logger trx \
        --collect:"XPlat Code Coverage" \
        --results-directory ${{ github.workspace }}/TestResults \
        --filter "$FILTER"
    else
      dotnet test -m:1 --no-build --verbosity normal --logger trx \
        --collect:"XPlat Code Coverage" \
        --results-directory ${{ github.workspace }}/TestResults
    fi
```

The `skip` output short-circuits the step entirely when it is set. This avoids even the overhead of loading and enumerating the test assemblies on runs where the only changes were documentation. Coverage collection, report generation, and the PR comment with the coverage summary are all guarded by the same condition — they only run when tests actually execute.

## Why `FullyQualifiedName~` works here

The vstest `~` operator is a contains match on the fully qualified test name, which for NUnit tests takes the form `Namespace.ClassName.MethodName`. Every test in Topaz lives in a namespace that includes its service name — `Topaz.Tests.E2E.ContainerRegistry.*`, `Topaz.Tests.AzureCLI.KeyVault.*`, and so on. The filter expression does not need to know about individual test classes or assemblies; matching on the service name substring is sufficient and stays correct as new test classes are added.

The `|` separator in the joined expression is the vstest OR operator, so `FullyQualifiedName~ContainerRegistry|FullyQualifiedName~Storage` matches any test whose name contains either substring — exactly what is needed when multiple services change in a single commit.

## The end result

A change to a single service endpoint runs only that service's tests. A change to shared infrastructure runs everything. A docs commit skips the pipeline entirely. Manual runs always cover the full suite. The coverage report and PR comment appear only when there is something to report.

The entire decision logic is a single shell step with no external service dependencies — no test impact analysis tool, no database of historical runs, no per-test timing data. It works because Topaz's project structure is already partitioned by service, and the test namespaces mirror that partition. The CI configuration just makes the correspondence explicit.
