---
sidebar_position: 3
---

# group list

List the resource groups in the subscription group

## Options
* `-s|--subscription-id` - (Required) subscription ID

## Examples

### List resource groups
```bash
$ topaz group list --subscription-id f7683160-34dc-4a94-9e66-eab2ad28e03a

[
  {
    "id": 
"/subscriptions/f7683160-34dc-4a94-9e66-eab2ad28e03a/resourceGroups/test",
    "name": "test",
    "type": "Microsoft.ResourceGroups/group",
    "location": "westeurope",
    "tags": {},
    "properties": {
      "provisioningState": "Created"
    }
  }
]
```
