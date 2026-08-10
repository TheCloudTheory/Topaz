---
sidebar_position: 2
---

# deployment group export-template
Exports an ARM template from a resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Resource group name.
* `-o, --options` - Export options: comma-separated list of IncludeParameterDefaultValue, IncludeComments, SkipResourceNameParameterization, SkipAllParameterization.

## Examples

### Export template from a resource group
```bash
$ topaz deployment group export-template \
    --name "my-rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A"
```

### Export template with parameterization options
```bash
$ topaz deployment group export-template \
    --name "my-rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A" \
    --options "IncludeParameterDefaultValue,IncludeComments"
```
