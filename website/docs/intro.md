---
sidebar_position: 1
description: Get started with Topaz, the open-source local Azure emulator. Install, configure DNS, trust the certificate, and run Azure Storage, Key Vault, Service Bus, and more — without a real Azure subscription.
keywords: [azure emulator, local azure, topaz, azure storage local, key vault local, azurite alternative, getting started]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Getting started

In this guide, we will install Topaz, configure DNS, trust the certificate, and verify the emulator is running on your machine.

Topaz is a local Azure emulator — a single process that runs Azure Storage, Key Vault, Service Bus, and more on localhost. See [How Topaz works](./concepts/how-topaz-works.md) for a conceptual overview of its architecture.

:::danger[Early stage]

Topaz is still in active development. Breaking changes may be introduced in upcoming releases. Check the release notes before upgrading.

:::

## Prerequisites

| | Standalone executable | Docker container |
|---|---|---|
| Runtime | None — self-contained binary | Docker (or compatible runtime) |
| Certificates | Must be installed & trusted | Handled automatically |
| DNS | One-time setup script required | One-time setup script required |

Releases are published on the [GitHub releases page](https://github.com/TheCloudTheory/Topaz/releases). Download the package that matches your platform and architecture.

:::tip[Windows users]

The recommended way to run Topaz on Windows is inside **WSL 2** (Windows Subsystem for Linux). This gives you a normal Linux environment and avoids certificate and DNS complications. All shell examples below assume a Linux/macOS shell; run them inside WSL if you're on Windows.

:::

## Step 1 — One-time DNS configuration

:::tip[macOS — install with Homebrew]

On macOS you can skip Steps 1 and 3 by installing via Homebrew. DNS setup and the `topaz-host` and `topaz` binaries are handled automatically:

```bash
brew tap thecloudtheory/topaz
brew install topaz
```

You will still need to complete **Step 2** (trusting the certificate). The certificate is installed at `$(brew --prefix)/bin/topaz.crt`.

:::

:::tip[Linux — shell installer or Homebrew on Linux]

On Linux you can skip Step 3 by using the shell installer. It detects your architecture, downloads the correct `topaz` and `topaz-host` binaries from the latest GitHub release, and places them on your `PATH`:

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash
```

To pin a specific version, set `TOPAZ_VERSION` before piping:

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | TOPAZ_VERSION=v1.1-beta.3 bash
```

Alternatively, if you already have [Homebrew on Linux](https://docs.brew.sh/Homebrew-on-Linux) installed, the same macOS tap works without any changes:

```bash
brew tap thecloudtheory/topaz
brew install topaz
```

Either way, you still need to complete **Steps 1 and 2** (DNS and certificate trust).

:::

Topaz emulates Azure service hostnames (e.g. `*.blob.core.windows.net`) locally. A one-time DNS configuration is required so that these hostnames resolve to `127.0.0.1`. This needs admin privileges, but once done Topaz needs none at runtime.

<Tabs groupId="os">
<TabItem value="macos" label="macOS">

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/install-macos.sh | sudo bash
```

Or clone the repo and run it directly:

```bash
sudo bash install/install-macos.sh
```

</TabItem>
<TabItem value="linux" label="Linux / WSL">

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/install-linux.sh | sudo bash
```

Or clone the repo and run it directly:

```bash
sudo bash install/install-linux.sh
```

</TabItem>
</Tabs>

You only need to run this script once per machine (or WSL instance).

## Step 2 — Trust the certificate

Topaz exposes HTTPS endpoints using a self-signed certificate that is bundled in the release package. You need to add it to your system trust store so that tools like the Azure CLI and the Azure SDKs can connect without TLS errors.

<Tabs groupId="os">
<TabItem value="macos" label="macOS">

Open Keychain Access, import `topaz.crt`, and mark it as **Always Trust**, or use the terminal:

```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain topaz.crt
```

</TabItem>
<TabItem value="linux" label="Linux / WSL">

```bash
sudo cp topaz.crt /usr/local/share/ca-certificates/topaz.crt
sudo update-ca-certificates
```

</TabItem>
</Tabs>

:::tip[Skip certificates with Docker]

When running Topaz as a Docker container you can expose only the HTTP data-plane ports and skip this step entirely. HTTPS is still available inside the container on port 8899 (ARM), but services like Blob and Table Storage use plain HTTP on their own ports.

:::

## Step 3 — Run the emulator

<Tabs groupId="runtime">
<TabItem value="homebrew" label="Homebrew">

```bash
topaz-host --log-level Information
```

The `topaz-host` and `topaz` binaries are already on your `PATH`. Open a second terminal and use `topaz` to manage resources.

### ROPC authentication (`az login --username --password`)

When the host starts, it also launches a built-in HTTP CONNECT proxy on port **44380**. This proxy is required for `az login --username --password` (Resource Owner Password Credentials).

Set the `HTTPS_PROXY` environment variable **once** before running any `az` commands that require authentication:

```bash
export HTTPS_PROXY=http://127.0.0.1:44380
az login --username alice@mytenant.onmicrosoft.com --password P@ssw0rd!
```

The proxy passes all non-Topaz `CONNECT` requests through to the real internet unchanged. Topaz prints a reminder with the correct command when it starts.

</TabItem>
<TabItem value="executable" label="Standalone executable">

<Tabs groupId="os">
<TabItem value="macos" label="macOS / Linux / WSL">

```bash
cd <download-directory>
chmod +x topaz-host-linux-x64   # use topaz-host-osx-x64 on macOS; topaz-host-linux-arm64 on ARM
./topaz-host-linux-x64 --log-level Information
```

Topaz is split into two executables:

| Binary | Purpose |
|---|---|
| `topaz-host-*` | Runs the emulator — start this first |
| `topaz-*` | CLI for managing resources in the running emulator |

Available **Host** binaries by platform:

| Platform | Binary |
|---|---|
| macOS (Apple Silicon) | `topaz-host-osx-arm64` |
| macOS (Intel) | `topaz-host-osx-x64` |
| Linux x64 | `topaz-host-linux-x64` |
| Linux ARM64 | `topaz-host-linux-arm64` |
| Windows | `topaz-host-win-x64.exe` |

Available **CLI** binaries by platform:

| Platform | Binary |
|---|---|
| macOS (Apple Silicon) | `topaz-osx-arm64` |
| macOS (Intel) | `topaz-osx-x64` |
| Linux x64 | `topaz-linux-x64` |
| Linux ARM64 | `topaz-linux-arm64` |
| Windows | `topaz-win-x64.exe` (or use the Linux binary inside WSL) |

</TabItem>
</Tabs>

### Setting up an alias (recommended)

:::note
If you installed via the shell installer or Homebrew, the binaries are already named `topaz` and `topaz-host` and placed on your `PATH`. You can skip this section.
:::

Typing the full binary name every time is tedious. Create a shell alias or move the binary to a directory on your `PATH`:

```bash
# Option A — symlink into /usr/local/bin
sudo ln -s "$(pwd)/topaz-host-linux-x64" /usr/local/bin/topaz-host
sudo ln -s "$(pwd)/topaz-linux-x64" /usr/local/bin/topaz

# Option B — add alias to your shell profile
echo 'alias topaz-host="/path/to/topaz-host-linux-x64"' >> ~/.zshrc   # macOS (Zsh)
echo 'alias topaz-host="/path/to/topaz-host-linux-x64"' >> ~/.bashrc  # Linux / WSL
source ~/.zshrc   # or ~/.bashrc
```

After this you can run `topaz-host --log-level Information` to start the emulator and `topaz` in a second terminal to manage resources.

### ROPC authentication (`az login --username --password`)

When the host starts, it also launches a built-in HTTP CONNECT proxy on port **44380**. This proxy is required for `az login --username --password` (Resource Owner Password Credentials) on non-Docker installs.

The Azure CLI uses MSAL, which performs a user-realm discovery request (`GET .../common/userrealm/{username}?api-version=1.0`) before the actual login. MSAL always targets port **443** for this request regardless of the authority URL you configured. Because Topaz binds port 443 only inside Docker (where root is available), the request fails with `ECONNREFUSED` on a local install. The built-in proxy solves this by intercepting the `CONNECT topaz.local.dev:443` tunnel and forwarding it to port 8899 where Topaz listens.

Set the `HTTPS_PROXY` environment variable **once** before running any `az` commands that require authentication:

<Tabs groupId="os">
<TabItem value="macos" label="macOS / Linux / WSL">

```bash
export HTTPS_PROXY=http://127.0.0.1:44380
az login --username alice@mytenant.onmicrosoft.com --password P@ssw0rd!
```

</TabItem>
<TabItem value="windows" label="Windows (cmd / PowerShell)">

```cmd
set HTTPS_PROXY=http://127.0.0.1:44380
az login --username alice@mytenant.onmicrosoft.com --password P@ssw0rd!
```

</TabItem>
</Tabs>

The proxy passes all non-Topaz `CONNECT` requests (for example Azure CLI telemetry to `dc.services.visualstudio.com`) through to the real internet unchanged. Topaz prints a reminder with the correct command when it starts.

</TabItem>
<TabItem value="docker" label="Docker">

```bash
docker pull thecloudtheory/topaz-host:<tag>

# Run with the most commonly used ports exposed
docker run --rm \
  -p 8899:8899 \   # ARM / Resource Manager (HTTPS)
  -p 443:443 \     # Key Vault — Azure CLI data plane (HTTPS)
  -p 8898:8898 \   # Key Vault — Azure SDK data plane (HTTPS)
  -p 8891:8891 \\ # Storage (Blob / Table / Queue / File — HTTPS)
  -p 8897:8897 \   # Event Hub (HTTP)
  -p 8888:8888 \   # Event Hub (AMQP)
  -p 8889:8889 \   # Service Bus (AMQP)
  -p 5671:5671 \   # Service Bus (AMQP/TLS)
  -p 8892:8892 \   # Container Registry data plane (HTTPS)
  thecloudtheory/topaz-host:<tag>
```

Image tags match the Git release tags. Expose only the ports you actually need.

:::tip[Nightly builds]

A `nightly` tag is published automatically every day from the `main` branch. Use it to get the latest unreleased features and fixes:

```bash
docker pull thecloudtheory/topaz-host:nightly
```

Nightly images are not guaranteed to be stable. For production use, pin to a release tag.

:::

:::info[Data persistence]

By default, all state is held in memory and lost when the container stops. Mount a volume to persist resources across restarts:

```bash
docker run --rm \
  -p 8899:8899 \
  -v topaz-data:/app/.topaz \
  thecloudtheory/topaz-host:<tag>
```

:::

</TabItem>
</Tabs>

## Step 4 — Verify the emulator is running

Once started, Topaz logs the list of bound endpoints. The quickest way to confirm everything is working is to run:

```bash
topaz health
```

![topaz health demo](/img/demos/verify-running.gif)

If the host is up, you'll see something like:

```
Host is running
  Status:    Healthy
  Directory: /home/user/my-project
  Port:      8899
```

You can also hit the health endpoint directly:

```bash
curl -k https://localhost:8899/health
# Expected: HTTP 200 with {"status":"Healthy", ...}
```

The `-k` flag skips TLS verification for the quick check. In normal usage the certificate will be trusted after Step 2, so you won't need it.

## Next steps

- [Supported services](./supported-services.md) — coverage matrix and port reference
- [Using Topaz CLI](./using-cli.md) — create subscriptions, resource groups, and more
- [Integrations](./integrations/azure-cli-integration.md) — Azure CLI, Terraform, ASP.NET Core, CI/CD, and more
- [Tutorials](./tutorials/local-terraform-development.md) — detailed, end-to-end guides with code and troubleshooting

