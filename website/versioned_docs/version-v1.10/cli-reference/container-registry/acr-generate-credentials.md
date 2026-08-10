---
sidebar_position: 5
---

# acr generate-credentials
Generates credentials for an Azure Container Registry token.

## Options
* `-s, --subscription-id` - (Required) Subscription ID.
* `-g, --resource-group` - (Required) Resource group name.
* `-n, --name` - (Required) Registry name.
* `--token-id` - Resource ID of the token for which credentials are generated.
* `--expiry` - Expiry date-time for the generated credentials (ISO 8601). Defaults to 1 year from now.
* `--password-name` - Specific password to regenerate: password1 or password2. Omit to regenerate both.

## Examples

### Generate credentials for a token
```bash
$ topaz acr generate-credentials \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --token-id "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/my-rg/providers/Microsoft.ContainerRegistry/registries/myregistry/tokens/myToken"
```
