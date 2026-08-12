---
sidebar_position: 4
description: Import real Azure resources into the Topaz emulator using the `topaz seed` command, with optional dry-run, resource group scoping, and resource type filtering.
keywords: [topaz seed, import azure resources, seed azure resources, topaz importer, local azure development, azure emulator seed]
---

# Import Azure resources

The `topaz seed` command pulls resource definitions from a live Azure subscription and creates matching resources inside the running Topaz emulator. This lets you start local development or testing with a realistic resource layout without recreating it by hand.

## Prerequisites

- Topaz installed and running (see [Getting started](./intro.md))
- Azure CLI installed and authenticated (`az login`)
- The Topaz CLI pointed at a running `topaz-host` instance

:::info[Azure credentials]
`topaz seed` reads from your real Azure subscription using the [`Azure.Identity` DefaultAzureCredential chain](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains?tabs=dac). Environment variables, the Azure CLI session, Managed Identity, and other credential sources are all picked up automatically. No credentials are written to Topaz — only resource definitions are imported.
:::

## Basic usage

Import all resources from a subscription:

```bash
topaz seed --subscription-id 00000000-0000-0000-0000-000000000001
```

Topaz fetches the subscription's resource groups and resources, creates them locally, and prints a summary table:

```
Importing resources. This may take a while.
╭──────────────────────┬───────╮
│ Property             │ Value │
├──────────────────────┼───────┤
│ Dry run              │ No    │
│ Resource groups      │ 3     │
│ Resources imported   │ 17    │
╰──────────────────────┴───────╯
```

## Scoping the import

### Filter by resource group

Import only the resources inside a single resource group:

```bash
topaz seed \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --resource-group rg-production
```

### Filter by resource type

Import only resources of a specific type:

```bash
topaz seed \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --resource-type Microsoft.Storage/storageAccounts
```

Both filters can be combined:

```bash
topaz seed \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --resource-group rg-production \
  --resource-type Microsoft.ServiceBus/namespaces
```

## Dry run

Preview what would be imported without writing anything to Topaz:

```bash
topaz seed \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --dry-run
```

The output table shows `Dry run: Yes` and lists the resources that *would* be created. No resources are written to the emulator.

## Overwriting existing resources

By default, `topaz seed` skips resources that already exist locally. Pass `--overwrite` to replace them:

```bash
topaz seed \
  --subscription-id 00000000-0000-0000-0000-000000000001 \
  --overwrite
```

:::caution
`--overwrite` performs `CreateOrUpdate` on existing resources in the emulator. Any local changes you made to those resources — configuration, data, keys — may be lost.
:::

## Full option reference

See [`topaz seed`](./cli-reference/generic/seed.md) in the CLI reference for a complete list of flags.

:::note[Known limitations]
RBAC role assignments and role definitions are not imported by `topaz seed`. You will need to recreate them manually using `topaz role-assignment create` or the equivalent Azure CLI commands pointed at Topaz.
:::
