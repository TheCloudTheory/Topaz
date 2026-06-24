---
sidebar_position: 11
---

# Azure Virtual Machines

> REST API reference: [Virtual Machines – 2025-04-01](https://learn.microsoft.com/en-us/rest/api/compute/virtual-machines?view=rest-compute-2025-04-01)

**Legend:** ✅ Implemented &nbsp;|&nbsp; ❌ Not implemented

## Control Plane

### Virtual Machines

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name}` — persisted to disk; `provisioningState` is always `Succeeded` |
| Get | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name}` |
| Delete | ✅ | `DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name}` — synchronous, returns `200 OK` |
| List | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines` — list by resource group |
| List All | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Compute/virtualMachines` — list by subscription |
| Assess Patches | ❌ | |
| Attach Detach Data Disks | ❌ | |
| Capture | ❌ | |
| Convert To Managed Disks | ❌ | |
| Deallocate | ❌ | |
| Generalize | ❌ | |
| Install Patches | ❌ | |
| Instance View | ❌ | |
| List Available Sizes | ❌ | |
| List By Location | ❌ | |
| Migrate To VM Scale Set | ❌ | |
| Perform Maintenance | ❌ | |
| Power Off | ❌ | |
| Reapply | ❌ | |
| Redeploy | ❌ | |
| Reimage | ❌ | |
| Restart | ❌ | |
| Retrieve Boot Diagnostics Data | ❌ | |
| Run Command | ❌ | |
| Simulate Eviction | ❌ | |
| Start | ❌ | |
| Update | ✅ | `PATCH /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name}` — partial update of tags and hardware/storage/network profiles without requiring a full PUT body |

### Virtual Machine Images

| Operation | Status | Notes |
|-----------|--------|-------|
| List | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Compute/locations/{location}/publishers/{publisher}/artifacttypes/vmimage/offers/{offer}/skus/{sku}/versions` — returns a stub version entry; satisfies `az vm create` image resolution |
| Get | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Compute/locations/{location}/publishers/{publisher}/artifacttypes/vmimage/offers/{offer}/skus/{sku}/versions/{version}` — returns stub version details with `plan: null` |
| List Publishers | ❌ | |
| List Offers | ❌ | |
| List Skus | ❌ | |

### Resource SKUs

| Operation | Status | Notes |
|-----------|--------|-------|
| List | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Compute/skus` — returns stub SKU list with `PremiumIO`, `OSVhdSizeMB`, `vCPUs`, `MemoryGB` capabilities; supports `$filter=location eq '...'`; used by ACE CapabilitiesCache |
