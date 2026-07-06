---
sidebar_position: 5
slug: /vscode-extension
description: Browse and manage Topaz resources directly from the VS Code sidebar. Monitor emulator health, stream logs, and create Azure resource hierarchy objects without leaving your editor.
keywords: [topaz vscode, vs code extension, azure emulator vscode, local azure vscode, topaz sidebar]
---

# VS Code Extension

:::warning[Experimental]
The VS Code extension is currently in an **experimental phase**. It is not compatible with Topaz versions prior to 1.9. Full support will be available once Topaz 1.9 is released.
:::

The Topaz VS Code extension lets you browse and manage Topaz resources — resource groups, storage accounts, Service Bus namespaces, and more — directly from the VS Code sidebar, without leaving your editor. You can also manage the emulator itself: monitor its health, version, and running mode, and create Azure resource hierarchy objects such as management groups, subscriptions, and resource groups directly from the sidebar.

## Prerequisites

Topaz must be running in the background before using this extension. Start it using one of the following methods:

```bash
# macOS
brew tap thecloudtheory/topaz && brew install topaz && topaz-host

# Linux
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash

# Docker (latest stable release)
docker run -p 8899:8899 thecloudtheory/topaz-host

# Docker (nightly — built daily from main)
docker run -p 8899:8899 thecloudtheory/topaz-host:nightly
```

## Installation

Install the extension from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=TheCloudTheory.topaz-azure-emulator).

## Settings

| Setting | Default | Description |
|---|---|---|
| `topaz.baseUrl` | `https://topaz.local.dev:8899` | Base URL of the running Topaz instance |
| `topaz.logSource` | `none` | Where to stream Topaz logs from: `none`, `file`, or `docker` |
| `topaz.logFile` | _(empty)_ | Absolute path to `topaz.log` (used when `logSource` is `file`) |
| `topaz.containerName` | `topaz.local.dev` | Docker container name to stream logs from (used when `logSource` is `docker`) |

By default, the extension connects to `https://topaz.local.dev:8899`. You can change this in VS Code settings under **Topaz > Base URL**.

## Log streaming

To stream logs from a **log file**, set `topaz.logSource` to `file` and point `topaz.logFile` at the `topaz.log` produced in Topaz's working directory.

To stream logs from a **Docker container**, set `topaz.logSource` to `docker` and make sure Topaz is started with the matching `--name` flag:

```bash
docker run -p 8899:8899 --name topaz.local.dev thecloudtheory/topaz-host
```

Logs appear in the **Output** panel (`⇧⌘U`) under the **Topaz Logs** channel.

## Features

- Browse resources grouped by resource group or by service type
- Create management groups, subscriptions, and resource groups
- Monitor emulator status: health, version, and running mode
- Stream Topaz logs from a local log file or a Docker container
- Refresh resource views on demand
