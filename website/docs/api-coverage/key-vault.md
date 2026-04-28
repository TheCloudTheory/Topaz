---
sidebar_position: 2
---

# Key Vault

:::info[Azure REST API reference]
- Control plane (ARM): [Azure Key Vault REST API · 2022-07-01](https://learn.microsoft.com/en-us/rest/api/keyvault/keyvault/operation-groups?view=rest-keyvault-keyvault-2022-07-01)
- Data plane – Secrets: [Secrets · 7.4](https://learn.microsoft.com/en-us/rest/api/keyvault/secrets/operation-groups?view=rest-keyvault-secrets-7.4)
- Data plane – Keys: [Keys · 7.4](https://learn.microsoft.com/en-us/rest/api/keyvault/keys/operation-groups?view=rest-keyvault-keys-7.4)
- Data plane – Certificates: [Certificates · 7.4](https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/operation-groups?view=rest-keyvault-certificates-7.4)
:::

This page tracks which Azure Key Vault REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (secrets, keys, certificates served on port 8898).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing vault resources.

### Vaults

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/keyvault/vaults?view=rest-keyvault-keyvault-2022-07-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Check Name Availability | ✅ | |
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| Get Deleted | ✅ | |
| List | ✅ | Via `GET /subscriptions/{id}/resources?$filter=...` |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| List Deleted | ✅ | |
| Purge Deleted | ✅ | |
| Recover Deleted | ✅ | |
| Update | ✅ | PATCH |
| Update Access Policy | ✅ | |

### Private Endpoint Connections

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/keyvault/private-endpoint-connections?view=rest-keyvault-keyvault-2022-07-01)

| Operation | Status |
|-----------|--------|
| Delete | ❌ |
| Get | ❌ |
| List By Resource | ❌ |
| Put | ❌ |

### Private Link Resources

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/keyvault/private-link-resources?view=rest-keyvault-keyvault-2022-07-01)

| Operation | Status |
|-----------|--------|
| List By Vault | ❌ |

---

## Data Plane

The data plane covers operations served directly from the vault's own hostname (e.g. `<vault-name>.vault.azure.net`) on port **8898** in Topaz. Secrets are fully implemented; the first Key operation is now available.

### Secrets

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/secrets/operation-groups?view=rest-keyvault-secrets-7.4)

| Operation | Status | Notes |
|-----------|--------|-------|
| Set Secret | ✅ | `PUT /secrets/{secretName}` |
| Get Secret | ✅ | By name and by name + version |
| Get Secrets | ✅ | Lists all secrets in the vault |
| Delete Secret | ✅ | |
| Update Secret | ✅ | `PATCH /secrets/{secretName}/{secretVersion}` |
| Get Secret Versions | ✅ | |
| Backup Secret | ✅ | |
| Restore Secret | ✅ | |
| Get Deleted Secret | ✅ | |
| Get Deleted Secrets | ✅ | |
| Recover Deleted Secret | ✅ | |
| Purge Deleted Secret | ✅ | |

### Keys

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/keys/operation-groups?view=rest-keyvault-keys-7.4)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Key | ✅ | `POST /keys/{key-name}/create` — RSA and EC key types |
| Import Key | ✅ | `PUT /keys/{key-name}` — RSA and EC key types |
| Get Key | ✅ | `GET /keys/{key-name}` and `GET /keys/{key-name}/{version}` |
| Get Keys | ✅ | `GET /keys` — lists all keys (latest version of each) |
| Get Key Versions | ✅ | `GET /keys/{key-name}/versions` |
| Update Key | ✅ | `PATCH /keys/{key-name}/{key-version}` |
| Delete Key | ✅ | `DELETE /keys/{key-name}` — soft-delete |
| Backup Key | ✅ | `POST /keys/{key-name}/backup` |
| Restore Key | ✅ |
| Get Deleted Key | ✅ |
| Get Deleted Keys | ✅ |
| Recover Deleted Key | ✅ |
| Purge Deleted Key | ✅ |
| Rotate Key | ✅ |
| Get Key Rotation Policy | ✅ |
| Update Key Rotation Policy | ✅ |
| Get Random Bytes | ✅ | `POST /rng` |
| encrypt | ✅ | `POST /keys/{name}/{version}/encrypt` — RSA1_5, RSA-OAEP, RSA-OAEP-256 |
| decrypt | ✅ |
| sign | ✅ |
| verify | ✅ |
| wrap Key | ✅ | `POST /keys/{name}/{version}/wrapkey` — RSA1_5, RSA-OAEP, RSA-OAEP-256 |
| unwrap Key | ✅ |
| release | ✅ |
| Get Key Attestation | ❌ |

### Certificates

> [REST reference](https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/operation-groups?view=rest-keyvault-certificates-7.4)

| Operation | Status |
|-----------|--------|
| Create Certificate | ❌ |
| Import Certificate | ❌ |
| Get Certificate | ❌ |
| Get Certificates | ❌ |
| Get Certificate Versions | ❌ |
| Get Certificate Policy | ❌ |
| Update Certificate | ❌ |
| Update Certificate Policy | ❌ |
| Delete Certificate | ❌ |
| Get Certificate Operation | ❌ |
| Update Certificate Operation | ❌ |
| Delete Certificate Operation | ❌ |
| Merge Certificate | ❌ |
| Backup Certificate | ❌ |
| Restore Certificate | ❌ |
| Get Deleted Certificate | ❌ |
| Get Deleted Certificates | ❌ |
| Recover Deleted Certificate | ❌ |
| Purge Deleted Certificate | ❌ |
| Get Certificate Contacts | ❌ |
| Set Certificate Contacts | ❌ |
| Delete Certificate Contacts | ❌ |
| Get Certificate Issuer | ❌ |
| Get Certificate Issuers | ❌ |
| Set Certificate Issuer | ❌ |
| Update Certificate Issuer | ❌ |
| Delete Certificate Issuer | ❌ |
