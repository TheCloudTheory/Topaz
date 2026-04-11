---
sidebar_position: 1
description: Get started with Topaz, the open-source local Azure emulator. Install, configure DNS, trust the certificate, and run Azure Storage, Key Vault, Service Bus, and more — without a real Azure subscription.
keywords: [azure emulator, local azure, topaz, azure storage local, key vault local, azurite alternative, getting started]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Getting started

Let's discover what Topaz is and how to set it up for local Azure development.

## What is Topaz?

Topaz is an Azure emulator that lets you develop and test Azure-based applications without connecting to real cloud services. It implements the ARM control plane and the data planes of popular services such as Azure Storage, Key Vault, Service Bus, and more — all running locally on your machine.

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
<TabItem value="executable" label="Standalone executable">

<Tabs groupId="os">
<TabItem value="macos" label="macOS / Linux / WSL">

```bash
cd <download-directory>
chmod +x topaz-linux-x64          # use topaz-osx-x64 on macOS; topaz-linux-arm64 on ARM
./topaz-linux-x64 start --log-level Information
```

Available binaries by platform:

| Platform | Binary |
|---|---|
| macOS (Intel & Apple Silicon via Rosetta 2) | `topaz-osx-x64` |
| Linux x64 | `topaz-linux-x64` |
| Linux ARM64 | `topaz-linux-arm64` |
| Windows | `topaz-win-x64.exe` (or use the Linux binary inside WSL) |

</TabItem>
</Tabs>

### Setting up an alias (recommended)

Typing the full binary name every time is tedious. Create a shell alias or move the binary to a directory on your `PATH`:

```bash
# Option A — symlink into /usr/local/bin
sudo ln -s "$(pwd)/topaz-linux-x64" /usr/local/bin/topaz

# Option B — add alias to your shell profile
echo 'alias topaz="/path/to/topaz-linux-x64"' >> ~/.zshrc   # macOS (Zsh)
echo 'alias topaz="/path/to/topaz-linux-x64"' >> ~/.bashrc  # Linux / WSL
source ~/.zshrc   # or ~/.bashrc
```

After this you can simply run `topaz start --log-level Information` from any directory.

</TabItem>
<TabItem value="docker" label="Docker">

```bash
docker pull thecloudtheory/topaz-cli:<tag>

# Run with the most commonly used ports exposed
docker run --rm \
  -p 8899:8899 \   # ARM / Resource Manager (HTTPS)
  -p 443:443 \     # Key Vault — Azure CLI data plane (HTTPS)
  -p 8898:8898 \   # Key Vault — Azure SDK data plane (HTTPS)
  -p 8891:8891 \   # Blob Storage (HTTP)
  -p 8890:8890 \   # Table Storage (HTTP)
  -p 8897:8897 \   # Event Hub (HTTP)
  -p 8888:8888 \   # Event Hub (AMQP)
  -p 8889:8889 \   # Service Bus (AMQP)
  -p 5671:5671 \   # Service Bus (AMQP/TLS)
  -p 8892:8892 \   # Container Registry data plane (HTTPS)
  thecloudtheory/topaz-cli:<tag> start --log-level Information
```

Image tags match the Git release tags. Expose only the ports you actually need.

:::info[Data persistence]

By default, all state is held in memory and lost when the container stops. Mount a volume to persist resources across restarts:

```bash
docker run --rm \
  -p 8899:8899 \
  -v topaz-data:/app/.topaz \
  thecloudtheory/topaz-cli:<tag> start --log-level Information
```

:::

</TabItem>
</Tabs>

## Step 4 — Verify the emulator is running

Once started, Topaz logs the list of bound endpoints. You can also do a quick health check against the ARM endpoint:

```bash
curl -k https://localhost:8899/subscriptions
# Expected: HTTP 200 with an empty subscriptions list
```

The `-k` flag skips TLS verification for the quick check. In normal usage the certificate will be trusted after Step 2, so you won't need it.

## Next steps

- [Supported services](./supported-services.md) — coverage matrix and port reference
- [Using Topaz CLI](./using-cli.md) — create subscriptions, resource groups, and more
- [Integrations](./integrations/azure-cli-integration.md) — Azure CLI, Terraform, ASP.NET Core, CI/CD, and more
- [Tutorials](./tutorials/local-terraform-development.md) — detailed, end-to-end guides with code and troubleshooting

