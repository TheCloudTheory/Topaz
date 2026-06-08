---
sidebar_position: 12
---

# Azure Managed Disks

> REST API reference: [Disks – 2025-11-01](https://learn.microsoft.com/en-us/rest/api/compute/disks?view=rest-compute-2025-11-01)

**Legend:** ✅ Implemented &nbsp;|&nbsp; ❌ Not implemented

## Control Plane

### Disks

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ❌ | `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}` |
| Get | ❌ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}` |
| Delete | ❌ | `DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}` |
| Update | ❌ | `PATCH /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks/{name}` — update tags, diskSizeGB, SKU |
| List By Resource Group | ❌ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/disks` |
| List | ❌ | `GET /subscriptions/{sub}/providers/Microsoft.Compute/disks` |
| Grant Access | ❌ | `POST .../disks/{name}/beginGetAccess` — returns accessSAS URI stub |
| Revoke Access | ❌ | `POST .../disks/{name}/endGetAccess` |
