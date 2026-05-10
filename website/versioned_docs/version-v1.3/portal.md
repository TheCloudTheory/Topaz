---
sidebar_position: 7
description: Use Topaz Portal — a web UI for browsing and managing emulated Azure resources running in Topaz without writing any CLI commands.
keywords: [topaz portal, azure emulator ui, local azure portal, topaz web interface, azure portal local]
---

# Topaz Portal

Topaz Portal is a lightweight web UI that lets you browse and manage emulated Azure resources — subscriptions, resource groups, Key Vaults, and more — without writing CLI commands. It connects directly to the running Topaz emulator over HTTPS and is distributed exclusively as a Docker image.

:::info[Topaz must be running]

The Portal is a front-end for the emulator. Start `topaz-host` (or the `thecloudtheory/topaz-host` container) before launching the Portal, and make sure the one-time DNS and certificate setup described in [Getting started](./intro.md) has been completed.

:::

## Running the Portal

Pull and start the Portal container, binding it to a local port of your choice (8900 is used in the examples below):

```bash
docker run -d \
  --name topaz-portal \
  -p 8900:8080 \
  thecloudtheory/topaz-portal:latest
```

Open `http://localhost:8900` in your browser.

:::tip[HTTPS]

The Portal serves its own HTTPS endpoint on container port 8081. To use it, bind that port instead and trust the Topaz certificate as described in [Getting started](./intro.md):

```bash
docker run -d \
  --name topaz-portal \
  -p 8900:8081 \
  thecloudtheory/topaz-portal:latest
```

Then open `https://localhost:8900`.

:::

## Connecting to the emulator

By default the Portal expects the emulator's ARM endpoint at `https://topaz.local.dev:8899`. This is the default address when you run Topaz with its built-in DNS and certificate setup.

If your emulator is running at a different address, override the `Topaz__ArmBaseUrl` environment variable:

```bash
docker run -d \
  --name topaz-portal \
  -p 8900:8080 \
  -e Topaz__ArmBaseUrl=https://topaz.local.dev:8899 \
  thecloudtheory/topaz-portal:latest
```

## Available views

| Page | Description |
|---|---|
| **Subscriptions** | List all subscriptions registered in the emulator |
| **Resource Groups** | Browse resource groups within a subscription |
| **Resource Manager** | Inspect raw ARM resources |
| **Key Vault** | View Key Vault instances and their secrets, keys, and certificates |
| **Deployments** | Inspect ARM template deployment history |
| **Entra** | Browse Entra ID objects managed by the emulator |
| **Authorization** | View role assignments |

## Versioning

Portal images are tagged identically to the main Topaz release (e.g. `v1.0.500-alpha`). Always use a matching tag for the Portal and emulator to avoid compatibility issues:

```bash
# Start the emulator at a specific version
docker run -d --name topaz-host thecloudtheory/topaz-host:v1.0.500-alpha

# Start the Portal at the same version
docker run -d --name topaz-portal -p 8900:8080 \
  thecloudtheory/topaz-portal:v1.0.500-alpha
```

## Docker Compose example

Running both services together with Docker Compose is the recommended approach for local development:

```yaml
services:
  topaz-host:
    image: thecloudtheory/topaz-host:latest
    ports:
      - "8899:8899"   # ARM / Resource Manager
      - "8898:8898"   # Key Vault
      - "8891:8891"   # Blob Storage

  topaz-portal:
    image: thecloudtheory/topaz-portal:latest
    ports:
      - "8900:8080"
    environment:
      - Topaz__ArmBaseUrl=https://topaz.local.dev:8899
    depends_on:
      - topaz-host
```
