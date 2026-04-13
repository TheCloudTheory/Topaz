---
sidebar_position: 3
description: Learn how to use the Topaz CLI to start the local Azure emulator, create subscriptions, manage resource groups, and interact with emulated Azure services from the command line.
keywords: [topaz cli, azure emulator cli, local azure commands, topaz start, subscription management]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Using Topaz CLI

Topaz is a single binary that acts as both the emulator and a CLI. The same executable can start the local Azure environment **and** issue commands to manage resources within it.

## Running the emulator

Use the `start` command to bring up the emulator:

```bash
topaz start --log-level Information
```

When running as a Docker container the `start` command is the default entrypoint, so it can be omitted:

```bash
# These two are equivalent
docker run --rm -p 8899:8899 thecloudtheory/topaz-cli:<tag> start --log-level Information
docker run --rm -p 8899:8899 thecloudtheory/topaz-cli:<tag>
```

See the [`start` command reference](./cli-reference/emulator/start.md) for the full list of options.

### Useful startup flags

| Flag | Description |
|---|---|
| `--log-level` | Verbosity: `Debug`, `Information`, `Warning`, `Error` |
| `--default-subscription` | Creates a subscription with the given GUID at startup |
| `--tenant-id` | Required when integrating with Azure CLI (Entra ID auth) |
| `--enable-logging-to-file` | Persists log output to a file |
| `--refresh-log` | Clears the log file on each start |
| `--emulator-ip-address` | Override the listening IP (default `127.0.0.1`) |

### Starting with a default subscription

By default Topaz starts with no subscriptions. Pass `--default-subscription` to have one created automatically at startup — useful for automated environments and CI pipelines:

```bash
topaz start \
  --log-level Information \
  --default-subscription 00000000-0000-0000-0000-000000000001
```

### Logging to file

```bash
topaz start --log-level Debug --enable-logging-to-file --refresh-log
```

Logs are written to the `.topaz` directory inside the working folder.

## Bring-your-own-certificate (BYOC)

If you cannot trust the bundled self-signed certificate (e.g. in a corporate environment with its own CA), you can supply your own PEM-encoded certificate and private key:

<Tabs groupId="runtime">
<TabItem value="executable" label="Standalone executable">

```bash
topaz start \
  --certificate-file "/path/to/your/certificate.crt" \
  --certificate-key "/path/to/your/private.key"
```

:::warning[macOS limitation]

BYOC currently does not work on macOS when running the standalone executable because Topaz does not yet support custom PFX certificates on that platform. The recommended workaround is to run Topaz as a Docker container instead. See [issue #20](https://github.com/TheCloudTheory/Topaz/issues/20) for tracking.

:::

</TabItem>
<TabItem value="docker" label="Docker">

Mount the certificate and key into the container, then reference them via the flags:

```bash
docker run --rm \
  --name topaz \
  -p 8899:8899 \
  -v /path/to/your/certificate.crt:/app/certificate.crt:ro \
  -v /path/to/your/private.key:/app/private.key:ro \
  thecloudtheory/topaz-cli:<tag> \
  start --certificate-file "certificate.crt" --certificate-key "private.key"
```

</TabItem>
</Tabs>

## Using the CLI to manage resources

While the emulator is running in the background, use the same binary (or a second terminal) to issue CLI commands against it. The CLI communicates with the locally running emulator over HTTP.

### Quick-start: subscription and resource group

The most common first steps are creating a subscription and a resource group:

```bash
# Create a subscription
topaz subscription create \
  --id 00000000-0000-0000-0000-000000000001 \
  --name "dev-local"

# Create a resource group inside it
topaz group create \
  --name "rg-my-app" \
  --location "westeurope" \
  --subscription-id 00000000-0000-0000-0000-000000000001
```

### Listing available commands

```bash
topaz -h          # top-level help
topaz <command> -h  # help for a specific command
```

### Examples by service

<Tabs groupId="service">
<TabItem value="keyvault" label="Key Vault">

```bash
# Create a Key Vault
topaz keyvault create \
  --name "kv-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --location "westeurope"

# Delete a Key Vault
topaz keyvault delete \
  --name "kv-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001
```

</TabItem>
<TabItem value="servicebus" label="Service Bus">

```bash
# Create a Service Bus namespace
topaz servicebus namespace create \
  --name "sb-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --location "westeurope"

# Create a queue inside it
topaz servicebus queue create \
  --name "my-queue" \
  --namespace-name "sb-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001
```

</TabItem>
<TabItem value="eventhub" label="Event Hub">

```bash
# Create an Event Hub namespace
topaz eventhubs namespace create \
  --name "eh-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --location "westeurope"

# Create an Event Hub inside it
topaz eventhubs eventhub create \
  --name "my-hub" \
  --namespace-name "eh-local" \
  --resource-group "rg-my-app" \
  --subscription-id 00000000-0000-0000-0000-000000000001
```

</TabItem>
</Tabs>

For the complete list of commands and their parameters, browse the **CLI Reference** section in the left sidebar.
