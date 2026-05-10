---
sidebar_position: 8
---

# storage blob update
Updates the HTTP properties (content type, encoding, etc.) of a blob.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `--content-type` - Content-Type header value (e.g. text/plain).
* `--content-encoding` - Content-Encoding header value.
* `--content-language` - Content-Language header value.
* `--cache-control` - Cache-Control header value.
* `--content-disposition` - Content-Disposition header value.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Update content type of a blob
```bash
$ topaz storage blob update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt" \
    --content-type "text/plain"
```
