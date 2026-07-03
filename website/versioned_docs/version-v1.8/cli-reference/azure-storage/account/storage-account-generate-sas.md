---
sidebar_position: 4
---

# storage account generate-sas
Generates a Shared Access Signature (SAS) token for a storage account.

## Options
* `-n, --account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--services` - (Required) (Required) The storage services to sign (b=blob, f=file, q=queue, t=table). Combine letters as needed.
* `--resource-types` - (Required) (Required) The resource types accessible with the SAS (s=service, c=container, o=object).
* `--permissions` - (Required) (Required) The permissions granted by the SAS (e.g. rwdlacup).
* `--expiry` - (Required) (Required) Expiry date/time in UTC (ISO 8601, e.g. 2030-01-01T00:00:00Z).
* `--start` - Start date/time in UTC (ISO 8601).
* `--https-only` - Restrict to HTTPS connections only.
* `--ip` - Restrict to a specific IP address or range.
* `--key-to-sign` - The storage account key to use for signing (key1 or key2).

## Examples

### Generate SAS token
```bash
$ topaz storage account generate-sas \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --services "b" \
    --resource-types "sco" \
    --permissions "rwdlacup" \
    --expiry "2030-01-01T00:00:00Z"
```
