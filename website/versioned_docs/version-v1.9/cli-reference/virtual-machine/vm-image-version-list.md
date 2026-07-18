---
sidebar_position: 5
---

# vm image-version list
Lists Azure Virtual Machine image versions.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-l, --location` - (Required) (Required) Location.
* `-p, --publisher` - (Required) (Required) Publisher.
* `-o, --offer` - (Required) (Required) Offer.
* `--sku` - (Required) (Required) SKU.

## Examples

### Lists Virtual Machine image versions
```bash
$ topaz vm image-version list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --location "westeurope" \
    --publisher "Canonical" \
    --offer "0001-com-ubuntu-server-focal" \
    --sku "20_04-lts-gen2"
```
