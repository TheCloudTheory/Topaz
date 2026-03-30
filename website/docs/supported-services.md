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
Key Vault|✅|⚠️
Event Hub|⚠️|⚠️
Service Bus|⚠️|⚠️
Virtual Network|⚠️|N/A
Azure Resource Manager|⚠️|N/A
Managed Identity|✅|N/A
Container Registry|✅|:x:
Azure SQL|:x:|:x:
Entra ID|:x:|⚠️
RBAC|⚠️|:x:
Monitor|⚠️|:x:

✅ - fully supported (stable)

⚠️ - partially supported (unstable)

:x: - not supported

`N/A` - not provided by Azure

:::tip[Looking for a detailed breakdown?]

The [API Coverage](./api-coverage/) section lists every REST operation for each service and shows exactly which ones are implemented in Topaz.

:::

## Used ports
The ports used by Topaz can be divided into two groups:
* common port for Azure Resource Manager operations
* service-specific port for data plane

You can find which service uses which port below:
Service Name|Port|Protocol
------------|----|--------
Resource Manager|8899, 443|HTTPS
Table Storage|8890|HTTP
Blob Storage|8891|HTTP
Azure Key Vault|8898|HTTPS
Azure Event Hub|8897|HTTPS
Azure Event Hub (AMQP)|8888|AMQP
Azure Service Bus|8887, 8899|HTTPS
Azure Service Bus (AMQP)|8889, 5671|AMQP, AMQP/TLS

For HTTPS endpoints, if you're running Topaz as a standalone application, you need to install and trust the certificates provided along with the main package.