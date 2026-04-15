---
sidebar_position: 1
---

# role assignment create
Creates (or updates) an Azure RBAC role assignment for a principal at a given scope.

## Options
* `-n, --name` - (Required) role assignment name (GUID). This becomes the roleAssignment resource name.
* `-d, --role-definition-id` - (Required) role definition ID. Example: `/providers/Microsoft.Authorization/roleDefinitions/<roleGuid>`.
* `-p, --principal-id` - (Required) principal (object) ID in Entra ID (GUID).
* `-t, --principal-type` - (Required) principal type. Common value: ServicePrincipal.
* `--scope` - (Required) scope for the role assignment. Example: `/subscriptions/<subId>` or a resource ID.
* `-s, --subscription-id` - (Required) subscription ID (GUID).

## Examples

### Assign Reader at subscription scope to a managed identity (or service principal)
```bash
$ topaz role assignment create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name 11111111-2222-3333-4444-555555555555 \
    --role-definition-id acdd72a7-3385-48ef-bd42-f606fba81ae7 \
    --principal-id 66666666-7777-8888-9999-000000000000 \
    --principal-type ServicePrincipal \
    --scope /subscriptions/36a28ebb-9370-46d8-981c-84efe02048ae
```

### Assign Key Vault Secrets User at Key Vault scope (data-plane secrets access)
```bash
$ topaz role assignment create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee \
    --role-definition-id 4633458b-17de-408a-b874-0445c86b69e6 \
    --principal-id 66666666-7777-8888-9999-000000000000 \
    --principal-type ServicePrincipal \
    --scope /subscriptions/36a28ebb-9370-46d8-981c-84efe02048ae/resourceGroups/rg-local/providers/Microsoft.KeyVault/vaults/mykv
```
