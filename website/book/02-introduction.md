# Introduction

Building an application for Microsoft Azure usually involves more than writing application code. A developer may need to create cloud resources, configure identities and permissions, connect service clients, and keep development and test environments usable. Repeating those steps for every change can slow feedback and make routine testing depend on a shared cloud environment.

Topaz provides a local environment for developing and testing applications that use selected Azure services. It runs on a developer's machine or in a container and implements Azure-compatible management and service APIs. Applications and tools can send requests to Topaz instead of to a live Azure resource.

This book explains how to use Topaz as part of a local development workflow. It begins with the concepts and setup requirements, then moves toward creating resources, connecting application code, and testing behavior.

## What emulation means

An emulator implements an API or system locally so that software can interact with it without using the remote service. In this context, Topaz accepts requests intended for selected Azure APIs and handles them on the local machine. The application can create resources and use service endpoints without requiring those operations to reach an Azure subscription.

Azure APIs have two broad roles:

- The **control plane** creates and manages resources. For example, it creates a resource group or provisions a storage account.
- The **data plane** operates on a resource. For example, it writes a blob or retrieves a secret.

Topaz supports control-plane and data-plane operations for several Azure services. The exact operations and their maturity vary by service and Topaz release. Check the current [supported services and API coverage](https://topaz.thecloudtheory.com/docs/supported-services/) before relying on a specific operation.

## What emulation does not guarantee

An emulator is not a second production Azure region. Topaz implements a defined set of APIs; it does not make every Azure service or operation available. Even when an operation is supported, local behavior may differ from Azure in areas such as service limits, timing, scale, regional behavior, or other platform characteristics.

Use Topaz to develop and test the behavior it supports. Before release, validate the application against the Azure services and configuration it will use in production. Consult the API coverage documentation when a test depends on a particular operation. Do not treat a passing local test as proof of production availability, security, capacity, or performance.

Topaz persists emulator state to disk. When it runs in a container, retaining that state beyond the container's lifetime depends on how storage is configured for the container. Local resources and data are useful for development, but they are not a substitute for production backup, access control, monitoring, or recovery arrangements.

## How Topaz fits into a workflow

Topaz is made up of two components that work together:

| Program | Role |
|---|---|
| `Host` | Runs the local emulator and its service endpoints. |
| `Topaz CLI` | Checks the host and manages resources in the emulator. |

Normally, you run the host in the background and use an Azure SDK or tool, such as Azure CLI or Azure PowerShell, to interact with the emulated environment. The Topaz CLI is optional; it provides direct commands to check the host and manage resources without requiring a separate Azure tool.

These examples depend on the services and operations supported by the Topaz version you use. Check the current [supported services and API coverage](https://topaz.thecloudtheory.com/docs/supported-services/) before designing a workflow around a specific operation.

Topaz routes Azure service hostnames to the local machine using a one-time DNS configuration. HTTPS clients also need to trust the certificate used by Topaz. After setup, tools configured for Topaz send their requests to local endpoints rather than to Azure. This arrangement can make development and automated tests less dependent on a deployed Azure environment. It also provides a place to create disposable local resources and exercise application code against service APIs. Here are some ways you could use Topaz in your work:

- Run application integration tests against local Azure-compatible services, including in a continuous integration pipeline.
- Develop and debug applications that use Azure SDKs without sending those service requests to a live Azure resource.
- Deploy infrastructure definitions with ARM, Bicep, or Terraform, then test the resulting resources and permissions locally.
- Prototype a feature or test a microservices workflow that depends on several emulated Azure services.
- Develop AI agents that call Azure services, or test tools that create and inspect resources in Topaz.
- Test authorization behavior, error handling, and retry policies against the features supported by Topaz.
- Help developers learn Azure SDK and resource-management workflows in a local environment.


## How to use this book

The first chapters introduce Topaz and establish the prerequisites for running it. Later chapters will use that foundation to configure a local environment and connect development tools and application code.

The examples are intended for local development and testing. Keep local configuration separate from production configuration, and use credentials and test data appropriate for a local environment. Where an example depends on a service feature, verify that feature in the current coverage documentation for the Topaz version in use.
