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
