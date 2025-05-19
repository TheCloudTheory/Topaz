# Topaz
<div align="center">
  <img src="https://github.com/TheCloudTheory/Topaz/blob/main/static/topaz-logo-small.png" />
  
  <b>Local Azure environment emulation for development</b>
</div>

## What is Topaz?
Topaz is an Azure emulator, which allows you to develop Azure-based applications without a need to connect to cloud services. It mimics popular Azure components such as Azure Storage, Azure Key Vault or Azure Service Bus to provide a robust local environment. 

Note that Topaz is in early stage of its development and each new version may introduce breaking changes to the provided interface.

## Supported services
Service Name|Control Plane|Data Plane
------------|-------------|----------
Subscriptions|⚠️|N/A
Resource Groups|⚠️|N/A
Azure Storage|⚠️|N/A
Table Storage|⚠️|⚠️
Queue Storage|:x:|:x:
Key Vault|⚠️|⚠️


## Installation

### Local certificate
To be able to interact with HTTPS endpoints provided by the emulator, you will need to install and trust the certificate locally. The certificate (`localhost.pfx`) is available in the `Topaz.Host` project.

## Differences

The differences between the emulator and the actual service.

### Table Service
* `Update` and `Merge` operations are performed in the same way, i.e. emulator always replaces an entity, even if `Merge` is requested

## Detailed support (REST APIs)

### Table Service
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Table Service|Set Table Service Properties|:x:|-
Table Service|Get Table Service Properties|✅|-
Table Service|Preflight Table Request|:x:|-
Table Service|Get Table Service Stats|:x:|-
Table Service|Query Tables|✅|Doesn't include limitations for queries
Table Service|Create Table|✅|-
Table Service|Delete Table|✅|In emulator deletion is immediate so no HTTP 409 is likely to happen
Table Service|Get Table ACL|✅|-
Table Service|Set Table ACL|✅|-
Table Service|Query Entities|✅|Doesn't support OData parameters
Table Service|Insert Entity|✅|-
Table Service|Update Entity|✅|-
Table Service|Merge Entity|✅|-
Table Service|Delete Entity|✅|-
Table Service|Insert Or Replace Entity|✅|-
Table Service|Insert Or Merge Entity|✅|-

### Blob Service
Service Name|Operation|Is supported?|Remarks
------------|---------|-------------|-------|
Blob Service|List Containers|:x:|-
Blob Service|Get Blob Service Properties|:x:|-
Blob Service|Set Blob Service Properties|:x:|-
Blob Service|Preflight Blob Request|:x:|-
Blob Service|Get Blob Service Stats|:x:|-
Blob Service|Get Account Information|:x:|-
Blob Service|Get User Delegation Key|:x:|-
Blob Service|Create Container|:x:|-
Blob Service|Get Container Properties|:x:|-
Blob Service|Get Container Metadata|:x:|-
Blob Service|Set Containter Metadata|:x:|-
Blob Service|Get Container ACL|:x:|-
Blob Service|Set Container ACL|:x:|-
Blob Service|Delete Container|:x:|-
Blob Service|Lease Container|:x:|-
Blob Service|Restore Container|:x:|-
Blob Service|List Blobs|:x:|-
Blob Service|Find Blobs By Tag In Container|:x:|-
Blob Service|Put Blob|:x:|-
Blob Service|Put Blob From URL|:x:|-
Blob Service|Get Blob|:x:|-
Blob Service|Get Blob Properties|:x:|-
Blob Service|Set Blob Properties|:x:|-
Blob Service|Get Blob Metadata|:x:|-
Blob Service|Set Blob Metadata|:x:|-
Blob Service|Get Blob Tags|:x:|-
Blob Service|Set Blob Tags|:x:|-

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