---
sidebar_position: 6
slug: /cosmos-db-vscode-extension
description: Browse and query Cosmos DB databases and containers from VS Code using the Azure Databases extension — works with both Topaz local emulator accounts and real Azure Cosmos DB accounts.
keywords: [cosmos db vscode, azure databases extension, cosmos db local, topaz cosmos db, vs code cosmos db explorer]
---

# Cosmos DB Explorer in VS Code

The [Azure Databases](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-cosmosdb) extension for VS Code lets you browse databases and containers, run queries, and inspect documents — all from the sidebar. It works with both Topaz-emulated Cosmos DB accounts and real Azure Cosmos DB accounts.

## Prerequisites

- [Azure Databases extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-cosmosdb) installed in VS Code
- Topaz running with a Cosmos DB account created (for local emulator usage)
- Topaz certificate trusted at the OS level (see [Getting started](../intro.md))

## Trust the certificate in Node.js

The Azure Databases extension runs on Node.js, which does **not** automatically pick up certificates trusted at the OS level. You must set the `NODE_EXTRA_CA_CERTS` environment variable to point to the Topaz root CA certificate before launching VS Code.

Add the following to your shell profile (`~/.zshrc`, `~/.bashrc`, etc.):

```bash
export NODE_EXTRA_CA_CERTS="/path/to/topaz.crt"
```

Replace `/path/to/topaz.crt` with the actual path — for example `/usr/local/share/topaz/topaz.crt` on Linux, or the path where you cloned/installed Topaz.

After adding the variable, **restart VS Code** from the terminal (so it inherits the updated environment) for the change to take effect.

:::tip
On macOS, launching VS Code from Spotlight or the Dock may not pick up shell profile changes. Always launch from the terminal after updating environment variables:
```bash
code .
```
:::

## Connect to a Cosmos DB account

In the VS Code sidebar, open the **Azure Databases** panel and expand **Cosmos DB Accounts**.

### Local emulator

1. Expand **Local Emulators** → **NoSQL Emulator : 8895**.
2. Click **+ New Emulator Connection...**.
3. Paste the connection string from the Topaz Portal.

### Real Azure Cosmos DB account

1. Click **+ New Connection...** at the bottom of the **Cosmos DB Accounts** tree.
2. Paste the connection string from the Azure Portal (**Cosmos DB account → Keys → Primary Connection String**).

### Local emulator connection string

Retrieve the connection string from the Topaz Portal or from the ARM API. It follows this format:

```
AccountEndpoint=https://{account-name}.documents.topaz.local.dev:8895/;AccountKey={key};
```

For example:
```
AccountEndpoint=https://my-cosmos.documents.topaz.local.dev:8895/;AccountKey=4KnaN3jL...==;
```

### Real Azure Cosmos DB connection string

Use the connection string from the Azure Portal (**Cosmos DB account → Keys → Primary Connection String**):

```
AccountEndpoint=https://{account-name}.documents.azure.com:443/;AccountKey={key};
```

## Browse and query

Once connected, the account appears in the Databases tree. You can:

- **Expand** the account to see databases and containers
- **Right-click** a container and select **Open Query Editor** to run SQL queries
- Use standard Cosmos DB SQL syntax, for example:

```sql
SELECT TOP 10 * FROM c
SELECT * FROM c WHERE c.id = "my-document"
SELECT c.name, c.createdAt FROM c ORDER BY c.createdAt DESC OFFSET 0 LIMIT 20
```

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `Unable to verify the first certificate` | `NODE_EXTRA_CA_CERTS` not set or VS Code not restarted | Set the env var and relaunch VS Code from the terminal |
| `The Cosmos DB account could not be resolved` | Wrong account name in the connection string endpoint | Make sure the hostname matches the actual account name, e.g. `my-cosmos.documents.topaz.local.dev` |
| `ERR_CERT_AUTHORITY_INVALID` in browser | Certificate not installed in the OS trust store | Run `sudo security add-trusted-cert` (macOS) or `update-ca-certificates` (Linux) |
