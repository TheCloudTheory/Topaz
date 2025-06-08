# Topaz ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/TheCloudTheory/Topaz/ci-build-and-test.yml) ![GitHub Release](https://img.shields.io/github/v/release/TheCloudTheory/Topaz?include_prereleases)


<div align="center">
  <img src="https://github.com/TheCloudTheory/Topaz/blob/main/static/topaz-logo-small.png" />
  
  <b>Local Azure environment emulation for development</b>
</div>

## What is Topaz?
Topaz is an Azure emulator, which allows you to develop Azure-based applications without a need to connect to cloud services. It mimics popular Azure components such as Azure Storage, Azure Key Vault or Azure Service Bus to provide a robust local environment. 

Note that Topaz is in early stage of its development and each new version may introduce breaking changes to the provided interface.

## Is Topaz free?
Yes, currently Topaz is free of any charges and doesn't require registration. This will change in the future, though you'll be notified about that fact several releases prior to it coming into life.

## Why Topaz?
Topaz offers a simplified DevEx by tightly integrating with moderns and popular tools used in development. You no longer need multiple emulators to start integrating with Azure services locally - all you need is a single executable (or Docker container). The set of capabilities offered by Topaz can be compiled into this short list:
* Support for both control & data plane of services
* Full portability
* Seamless integration with Azure SDK
* One-tool-to-rule-them-all
* Dedicated helpers for simplified connection and authentication
* Emulation of Azure resources' hierarchy including subscriptions and resource groups

There's also a backlog of features planned for future releases:
* UI for easier management of resources
* Emulation of Azure RBAC
* Emulation of ARM deployments using ARM Templates / Bicep

## Alternatives
If you want to work with emulators for Azure services, you have a couple of options:
* Azurite - https://github.com/Azure/Azurite
* Azure Cosmos DB Emulator - https://github.com/Azure/azure-cosmos-db-emulator-docker
* Azure Service Bus Emulator - https://github.com/Azure/azure-service-bus-emulator-installer

## Supported services
Service Name|Control Plane|Data Plane
------------|-------------|----------
Subscriptions|⚠️|N/A
Resource Groups|⚠️|N/A
Azure Storage|⚠️|N/A
Table Storage|⚠️|⚠️
Blob Storage|⚠️|⚠️
Queue Storage|:x:|:x:
Key Vault|⚠️|⚠️
Event Hub|⚠️|⚠️

## Installation

### Local certificate
To be able to interact with HTTPS endpoints provided by the emulator, you will need to install and trust the certificate locally. The certificate (`localhost.pfx`) is available in the `Topaz.Host` project.

## Differences

The differences between the emulator and the actual service.

### Table Service
* `Update` and `Merge` operations are performed in the same way, i.e. emulator always replaces an entity, even if `Merge` is requested

## Detailed support (REST APIs)

### Table Storage
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Table Storage|Set Table Service Properties|:x:|-
Table Storage|Get Table Service Properties|✅|-
Table Storage|Preflight Table Request|:x:|-
Table Storage|Get Table Service Stats|:x:|-
Table Storage|Query Tables|✅|Doesn't include limitations for queries
Table Storage|Create Table|✅|-
Table Storage|Delete Table|✅|In emulator deletion is immediate so no HTTP 409 is likely to happen
Table Storage|Get Table ACL|✅|-
Table Storage|Set Table ACL|✅|-
Table Storage|Query Entities|✅|Doesn't support OData parameters
Table Storage|Insert Entity|✅|-
Table Storage|Update Entity|✅|-
Table Storage|Merge Entity|✅|-
Table Storage|Delete Entity|✅|-
Table Storage|Insert Or Replace Entity|✅|-
Table Storage|Insert Or Merge Entity|✅|-

### Blob Storage
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Blob Storage|List Containers|✅|Doesn't support additional query parameters
Blob Storage|Get Blob Service Properties|:x:|-
Blob Storage|Set Blob Service Properties|:x:|-
Blob Storage|Preflight Blob Request|:x:|-
Blob Storage|Get Blob Service Stats|:x:|-
Blob Storage|Get Account Information|:x:|-
Blob Storage|Get User Delegation Key|:x:|-
Blob Storage|Create Container|✅|Doesn't respect access type and metadata parameters
Blob Storage|Get Container Properties|:x:|-
Blob Storage|Get Container Metadata|:x:|-
Blob Storage|Set Containter Metadata|:x:|-
Blob Storage|Get Container ACL|:x:|-
Blob Storage|Set Container ACL|:x:|-
Blob Storage|Delete Container|:x:|-
Blob Storage|Lease Container|:x:|-
Blob Storage|Restore Container|:x:|-
Blob Storage|List Blobs|✅|-
Blob Storage|Find Blobs By Tag In Container|:x:|-
Blob Storage|Put Blob|✅|Doesn't support yet different semantics of different blob types
Blob Storage|Put Blob From URL|:x:|-
Blob Storage|Get Blob|:x:|-
Blob Storage|Get Blob Properties|✅|-
Blob Storage|Set Blob Properties|:x:|-
Blob Storage|Get Blob Metadata|:x:|-
Blob Storage|Set Blob Metadata|✅|-
Blob Storage|Get Blob Tags|:x:|-
Blob Storage|Set Blob Tags|:x:|-
Blob Storage|Delete Blob|✅|-

### Key Vault
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Key Vault|Backup Secret|:x:|-
Key Vault|Delete Secret|✅|Secrets are immediatelly purged with no way to restore them
Key Vault|Get Deleted Secret|:x:|-
Key Vault|Get Deleted Secrets|:x:|-
Key Vault|Get Secret|✅|-
Key Vault|Get Secret Versions|:x:|-
Key Vault|Get Secrets|✅|-
Key Vault|Purge Deleted Secret|:x:|-
Key Vault|Recover Deleted Secret|:x:|-
Key Vault|Restore Secret|:x:|-
Key Vault|Set Secret|✅|-
Key Vault|Update Secret|:x:|-

### Event Hub
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Event Hub Namespace|Create Namespace|✅|Does not support full ARM definition
Event Hub|Create Hub|✅|Does not support full ARM definition
Event Hub|Send Message (AMQP)|✅|Does not support batches
