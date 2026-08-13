---
sidebar_position: 1
---

# context switch
Changes the active cloud environment context.

## Options
* `-n, --name` - Name of the context to switch to
* `--use-topaz` - Use the default context provided by Topaz
* `--use-default` - Use the default context provided by Azure CLI

## Examples

### Switch the context by selecting one from the list
```bash
$ topaz context switch
```

### Switch the context by using the one provided by the parameter
```bash
$ topaz context switch -n Topaz
```
