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
Service Bus|⚠️|⚠️
Virtual Network|⚠️|N/A
Azure Resource Manager|⚠️|N/A
Container Registry|:x:|:x:
Azure SQL|:x:|:x:

✅ - fully supported (stable)

⚠️ - partially supported (unstable)

:x: - not supported

`N/A` - not provided by Azure

## Used ports
The ports used by Topaz can be divided into two groups:
* common port for Azure Resource Manager operations
* service-specific port for data plane

You can find which service uses which port below:
Service Name|Port|Protocol
------------|----|--------
Resource Manager|8899|HTTPS
Table Storage|8890|HTTP
Azure Key Vault|8898|HTTPS
Azure Event Hub|8897, 8888|HTTP, AMQP
Blob Storage|8891|HTTP

For HTTPS endpoints, if you're running Topaz as a standalone application, you need to install and trust the certificates provided along with the main package.