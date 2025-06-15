---
sidebar_position: 2
---

# Supported services
Depending on the version of Topaz a different set of Azure services is supported for emulation. The table below presents the current state of emulation and maturity of certain features.

:::tip[Best practice]

Make sure you're using the most recent version of Topaz to benefit from the bugfixes and newest features.

:::

Service Name|Control Plane|Data Plane
------------|-------------|----------
Subscriptions|⚠️|N/A
Resource Groups|⚠️|N/A
Azure Storage|⚠️|⚠️
Table Storage|⚠️|✅
Blob Storage|⚠️|⚠️
Queue Storage|:x:|:x:
Key Vault|⚠️|⚠️
Event Hub|⚠️|⚠️

✅ - fully supported (stable)

⚠️ - partially supported (unstable)

:x: - not supported

`N/A` - not provided by Azure