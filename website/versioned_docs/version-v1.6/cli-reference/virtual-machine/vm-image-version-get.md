---
sidebar_position: 4
---

# vm image-version get
Gets an Azure Virtual Machine image version.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-l, --location` - (Required) (Required) Location.
* `-p, --publisher` - (Required) (Required) Publisher.
* `-o, --offer` - (Required) (Required) Offer.
* `--sku` - (Required) (Required) SKU.
* `-v, --version` - (Required) (Required) Version.

## Examples

### Gets a Virtual Machine image version
```bash
$ topaz vm image-version get --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --location "westeurope" \
    --publisher "Canonical" \
    --offer "0001-com-ubuntu-server-focal" \
    --sku "20_04-lts-gen2" \
    --version "20.04.202208100"
```
