# Topaz ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/TheCloudTheory/Topaz/ci-build-and-test.yml) ![GitHub Release](https://img.shields.io/github/v/release/TheCloudTheory/Topaz?include_prereleases)


<div align="center">
  <img src="./static/topaz-logo.png" />
  
  <b>Local Azure environment emulation for development</b>
</div>

## What is Topaz?
Topaz is an Azure emulator, which allows you to develop Azure-based applications without a need to connect to cloud services. It mimics popular Azure components such as Azure Storage, Azure Key Vault or Azure Service Bus to provide a robust local environment. 

Note that Topaz is in early stage of its development and each new version may introduce breaking changes to the provided interface.

Check [documentation](http://topaz.thecloudtheory.com/) for the guides, recipes and knowledge base about the project.

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
* Emulation of ARM deployments using ARM Templates / Bicep

There's also a backlog of features planned for future releases:
* UI for easier management of resources
* Emulation of Azure RBAC

## Alternatives
If you want to work with emulators for Azure services, you have a couple of options:
* Azurite - https://github.com/Azure/Azurite
* Azure Cosmos DB Emulator - https://github.com/Azure/azure-cosmos-db-emulator-docker
* Azure Service Bus Emulator - https://github.com/Azure/azure-service-bus-emulator-installer
