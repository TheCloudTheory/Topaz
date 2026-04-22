---
sidebar_position: 5
---

# storage account generate-service-sas
Generates a service-level Shared Access Signature (SAS) token for a storage account resource.

## Options
* `-n, --account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--canonicalized-resource` - (Required) (Required) Canonicalized path of the resource (e.g. /blob/accountname/containername).
* `--resource` - (Required) (Required) Signed resource type (b=blob, c=container, f=file, s=share).
* `--permissions` - (Required) (Required) The permissions granted by the SAS (e.g. rwdl).
* `--expiry` - (Required) (Required) Expiry date/time in UTC (ISO 8601, e.g. 2030-01-01T00:00:00Z).
* `--start` - Start date/time in UTC (ISO 8601).
* `--https-only` - Restrict to HTTPS connections only.
* `--ip` - Restrict to a specific IP address or range.
* `--identifier` - Stored access policy identifier.
* `--key-to-sign` - The storage account key to use for signing (key1 or key2).

## Examples

### Generate service SAS token
```bash
$ topaz storage account generate-service-sas \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --canonicalized-resource "/blob/salocal/mycontainer" \
    --resource "c" \
    --permissions "rwdl" \
    --expiry "2030-01-01T00:00:00Z"
```
