---
sidebar_position: 25
---

# keyvault key random-bytes
Generates random bytes using the Key Vault random number generator.

## Options
* `-c, --count` - (Required) (Required) Number of random bytes to generate (1–128).

## Examples

### Generate 32 random bytes
```bash
$ topaz keyvault key random-bytes --count 32
```
