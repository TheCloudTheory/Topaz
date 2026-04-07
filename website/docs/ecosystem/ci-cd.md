---
sidebar_position: 6
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# CI/CD integration

Running your test suite against a live Topaz instance in CI requires three setup steps that mirror what you'd do locally: install the certificate, configure DNS, and start the emulator. This page shows complete, copy-paste ready examples for GitHub Actions and Azure DevOps Pipelines.

## How Topaz runs in CI

There are two approaches:

| Approach | When to use |
|---|---|
| **Container service** | Simpler. Docker pulls the published image; no build step needed. |
| **In-process (executable)** | Fastest. Useful if you already publish a self-contained binary as a build artifact. |

Both approaches are shown below. The container approach is recommended for most projects.

## GitHub Actions

### Using the Docker container (recommended)

```yaml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      # 1 — Configure DNS so Azure SDK hostnames resolve to localhost
      - name: Configure DNS
        run: sudo bash install/install-linux.sh

      # 2 — Trust the Topaz certificate at the OS level
      - name: Install certificate
        run: sudo bash certificate/ubuntu-install.sh

      # 3 — Start Topaz as a background container
      - name: Start Topaz
        run: |
          docker run -d \
            --name topaz.local.dev \
            -p 8899:8899 \
            -p 8898:8898 \
            -p 8891:8891 \
            -p 8890:8890 \
            -p 8897:8897 \
            thecloudtheory/topaz-cli:${{ env.TOPAZ_VERSION }} \
            start --log-level Information
        env:
          TOPAZ_VERSION: v1.0.299-alpha   # pin to a specific release tag

      # 4 — Wait for the ARM endpoint to become ready
      - name: Wait for Topaz
        run: |
          for i in $(seq 1 30); do
            if curl -sk https://localhost:8899/subscriptions > /dev/null 2>&1; then
              echo "Topaz is ready"
              exit 0
            fi
            echo "Waiting... ($i/30)"
            sleep 2
          done
          echo "Topaz did not start in time" && exit 1

      - name: Restore & build
        run: dotnet restore && dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal
        env:
          TOPAZ_CLI_CONTAINER_IMAGE: thecloudtheory/topaz-cli:${{ env.TOPAZ_VERSION }}
```

### Using Testcontainers in tests (container managed by test code)

If your test fixtures use Testcontainers (see the [Testcontainers](./testcontainers.md) page), the container lifecycle is managed by the test code itself. You still need DNS and certificate setup, but you do **not** manually start the container in the workflow:

```yaml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Configure DNS
        run: sudo bash install/install-linux.sh

      - name: Install certificate
        run: sudo bash certificate/ubuntu-install.sh

      - name: Restore & build
        run: dotnet restore && dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal
        env:
          # Testcontainers reads this to pull the correct image
          TOPAZ_CLI_CONTAINER_IMAGE: thecloudtheory/topaz-cli:v1.0.299-alpha
```

### Using a locally built image

If your pipeline already builds a Topaz image from source (e.g. when testing changes to Topaz itself), pass it to your tests directly:

```yaml
      - name: Build Docker image
        uses: docker/build-push-action@v6
        with:
          context: .
          file: ./Topaz.CLI/Dockerfile
          push: false
          tags: topaz/cli
          platforms: linux/amd64

      - name: Test
        run: dotnet test --no-build --verbosity normal
        env:
          TOPAZ_CLI_CONTAINER_IMAGE: topaz/cli
```

### Publishing test results and coverage

Extend the test step with standard actions for reporting:

```yaml
      - name: Test
        run: |
          dotnet test --no-build --verbosity normal \
            --logger trx \
            --collect:"XPlat Code Coverage" \
            --results-directory TestResults

      - name: Publish test results
        uses: EnricoMi/publish-unit-test-result-action@v2
        if: always()
        with:
          files: "**/*.trx"

      - name: Coverage report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: TestResults/**/coverage.cobertura.xml
          targetdir: coveragereport
          reporttypes: Html;MarkdownSummaryGithub

      - name: Summary
        run: cat coveragereport/SummaryGithub.md >> $GITHUB_STEP_SUMMARY
```

## Azure DevOps Pipelines

```yaml
trigger:
  - main

pool:
  vmImage: ubuntu-latest

variables:
  TOPAZ_VERSION: v1.0.299-alpha

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: sudo bash install/install-linux.sh
    displayName: Configure DNS

  - script: sudo bash certificate/ubuntu-install.sh
    displayName: Install certificate

  - script: |
      docker run -d \
        --name topaz.local.dev \
        -p 8899:8899 \
        -p 8898:8898 \
        -p 8891:8891 \
        -p 8890:8890 \
        -p 8897:8897 \
        thecloudtheory/topaz-cli:$(TOPAZ_VERSION) \
        start --log-level Information
    displayName: Start Topaz

  - script: |
      for i in $(seq 1 30); do
        if curl -sk https://localhost:8899/subscriptions > /dev/null 2>&1; then
          echo "Topaz is ready"; exit 0
        fi
        echo "Waiting... ($i/30)"; sleep 2
      done
      echo "Topaz did not start in time" && exit 1
    displayName: Wait for Topaz

  - script: dotnet restore && dotnet build --no-restore
    displayName: Restore & build

  - script: |
      dotnet test --no-build --verbosity normal \
        --logger trx \
        --collect:"XPlat Code Coverage" \
        --results-directory $(Agent.TempDirectory)/TestResults
    displayName: Test
    env:
      TOPAZ_CLI_CONTAINER_IMAGE: thecloudtheory/topaz-cli:$(TOPAZ_VERSION)

  - task: PublishTestResults@2
    condition: always()
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '$(Agent.TempDirectory)/TestResults/**/*.trx'

  - task: PublishCodeCoverageResults@2
    inputs:
      summaryFileLocation: '$(Agent.TempDirectory)/TestResults/**/coverage.cobertura.xml'
```

## Pinning versions

Always pin the Topaz image tag in CI. Using `latest` can cause unexpected failures when a new release introduces breaking changes. A good pattern is to store the version in one place:

<Tabs>
<TabItem value="gha" label="GitHub Actions">

Use a workflow-level `env` or a repository variable:

```yaml
env:
  TOPAZ_VERSION: v1.0.299-alpha

# Reference it anywhere with ${{ env.TOPAZ_VERSION }}
```

Or use a [reusable workflow input](https://docs.github.com/en/actions/sharing-automations/reusing-workflows) so the calling workflow controls the version.

</TabItem>
<TabItem value="ado" label="Azure DevOps">

Define a pipeline variable and reference it with `$(TOPAZ_VERSION)`. For cross-pipeline consistency, store it as a Variable Group in the Library.

</TabItem>
</Tabs>

## Troubleshooting in CI

| Symptom | Likely cause | Fix |
|---|---|---|
| `CERTIFICATE_VERIFY_FAILED` | OS cert store not updated | Ensure `ubuntu-install.sh` runs before any `dotnet test` step |
| Connection refused on port 8899 | Container not ready yet | Add the readiness wait loop after `docker run` |
| `Waiting... (30/30)` timeout | Image pull too slow | Pre-pull with `docker pull` before `docker run`, or increase the retry count |
| DNS resolution failure | `install-linux.sh` not run | Confirm the script ran and check `/etc/hosts` on the runner |
| Tests pass locally but fail in CI | Port not exposed | Verify all required ports are in the `-p` list on `docker run` |
