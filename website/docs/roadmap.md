---
sidebar_position: 11
description: Topaz release roadmap — planned features and milestones across upcoming beta versions.
keywords: [topaz roadmap, azure emulator roadmap, planned features, upcoming releases]
---

# Roadmap

This page tracks what is planned for upcoming Topaz releases. The roadmap is derived from the [BACKLOG.md](https://github.com/TheCloudTheory/Topaz/blob/main/BACKLOG.md) file in the repository; each item there is automatically converted to a GitHub issue when committed.

:::info
The roadmap reflects current intentions and may change. Watch the [GitHub repository](https://github.com/TheCloudTheory/Topaz) or join [Discord](https://discord.gg/9eqCKe3N) to stay up to date.
:::

---

## v1.1-beta

### Key Vault — full secrets support

| Feature | Status | Description |
|---------|--------|-------------|
| Get Secret Versions | <span class="badge--stable">Stable</span> | `GET {vaultBaseUrl}/secrets/{name}/versions` — paged list of all versions for a secret |
| Backup & Restore | <span class="badge--stable">Stable</span> | `POST .../backup` and `POST .../restore` for opaque secret backup blobs |
| Deleted secrets operations | <span class="badge--stable">Stable</span> | Soft-delete surface: list, get, recover, and purge deleted secrets |

### Container Registry — data plane preview

| Feature | Status | Description |
|---------|--------|-------------|
| ACR OAuth2 token endpoint | <span class="badge--stable">Stable</span> | `POST /oauth2/token` — completes the three-step ACR authentication flow |
| Repositories & Tags | <span class="badge--preview">Preview</span> | List repositories (`/v2/_catalog`), list tags, delete tag via manifest reference |
| Manifest operations | <span class="badge--preview">Preview</span> | GET, PUT, DELETE, HEAD for OCI manifests per registry |
| Blob operations | <span class="badge--preview">Preview</span> | Full OCI blob upload/download: start session, stream chunks, complete, download, delete |

---

## v1.2-beta

### Queue Storage — preview

| Feature | Status | Description |
|---------|--------|-------------|
| Service-level operations | <span class="badge--preview">Preview</span> | List queues, get/set service properties, get stats |
| Queue CRUD | <span class="badge--preview">Preview</span> | Create, delete, get/set metadata and ACL per queue |
| Message operations | <span class="badge--preview">Preview</span> | Enqueue, dequeue, peek, update, delete, and clear messages |

### Key Vault — keys support

| Feature | Status | Description |
|---------|--------|-------------|
| Core CRUD | <span class="badge--stable">Stable</span> | Create, import, get, update, delete keys; list keys and versions |
| Backup & Restore | <span class="badge--stable">Stable</span> | Export and restore opaque key backup blobs |
| Cryptographic operations | <span class="badge--stable">Stable</span> | Encrypt, decrypt, sign, verify, wrap key, unwrap key |
| Key rotation | <span class="badge--preview">Preview</span> | Rotate key, get/update rotation policy |

### Azure PowerShell integration

| Feature | Status | Description |
|---------|--------|-------------|
| Certificate trust script | <span class="badge--stable">Stable</span> | `configure-azure-powershell-cert.ps1` — trust the Topaz certificate in the Az module |
| Cloud environment registration | <span class="badge--stable">Stable</span> | `Add-AzEnvironment` + `Connect-AzAccount` setup script and example |
| Test suite | <span class="badge--preview">Preview</span> | `Topaz.Tests.AzurePowerShell` project with a Testcontainers fixture and smoke tests |

### ARM Deployments — full support

| Feature | Status | Description |
|---------|--------|-------------|
| Cancel | <span class="badge--stable">Stable</span> | Mark an in-progress deployment as cancelled |
| Export Template | <span class="badge--stable">Stable</span> | Return the ARM template used for a deployment |
| What-If | <span class="badge--preview">Preview</span> | Preview resource changes without applying them |
| List at all scopes | <span class="badge--stable">Stable</span> | List deployments at subscription, management-group, and tenant scope |

---

## v1.3-beta

### Resource Providers — operations support

| Feature | Status | Description |
|---------|--------|-------------|
| List, Register, Unregister | <span class="badge--stable">Stable</span> | Full provider lifecycle alongside the existing get-by-namespace operation |

### Virtual Networks — full control plane

| Feature | Status | Description |
|---------|--------|-------------|
| Delete, List, Update Tags | <span class="badge--stable">Stable</span> | Complete the VNet control plane beyond create and get |
| Check IP Address Availability | <span class="badge--stable">Stable</span> | Validate whether an IP is available within a VNet's address space |
| Subnets — full CRUD | <span class="badge--stable">Stable</span> | Create, get, delete, and list subnets within a VNet |
| Network Security Groups | <span class="badge--preview">Preview</span> | Full NSG control plane: create, get, delete, list, update tags |

### Azure Virtual Machines — initial control plane

| Feature | Status | Description |
|---------|--------|-------------|
| New service scaffold | <span class="badge--stable">Stable</span> | `Topaz.Service.VirtualMachine` project with models, resource provider, and service registration |
| Core control plane | <span class="badge--preview">Preview</span> | Create/update, get, delete, list VMs — emulated only (no actual boot) |
