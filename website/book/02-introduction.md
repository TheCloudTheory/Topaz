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

Topaz routes Azure service hostnames to the local machine using a one-time DNS configuration. HTTPS clients also need to trust the certificate used by Topaz. After setup, tools configured for Topaz send their requests to local endpoints rather than to Azure. This provides a place to develop and test application workflows without directing those service requests to live Azure resources.

Here are some ways you could use Topaz in your work.

### Scenario 1: Run application integration tests

Run application tests against local instances of the Azure services they depend on. The same workflow can run in continuous integration, where each test run can use its own Topaz instance and resource state. This helps test interactions between application code and supported service APIs without creating those test resources in Azure.

::: {.scenario-diagram role="img" aria-label="Test runner sends application tests to the Topaz host, which handles them with emulated services."}
[Test runner: application tests]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Topaz host]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Emulated services]{.scenario-node}
:::

### Scenario 2: Develop and debug Azure SDK applications

Configure Azure SDK clients to send requests to Topaz while developing an application. This lets you exercise supported API calls and inspect application behavior against local resources. Keep the local endpoint and credential configuration separate from production settings.

::: {.scenario-diagram role="img" aria-label="An application uses an Azure SDK to call a Topaz endpoint and access local resources."}
[Application]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Azure SDK: Topaz endpoint]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Local resources]{.scenario-node}
:::

### Scenario 3: Validate infrastructure definitions

Deploy ARM templates, Bicep templates, or Terraform configurations to Topaz. Inspect the resources and permissions created by the deployment before using the same definitions against Azure. The checks you can perform depend on the resource types and operations supported by your Topaz version.

::: {.scenario-diagram role="img" aria-label="Infrastructure definitions are deployed through the Topaz ARM endpoint to create resources and permissions for verification."}
[ARM, Bicep, or Terraform]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Topaz ARM endpoint]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Resources and permissions]{.scenario-node}
:::

### Scenario 4: Prototype workflows across services

Build a feature that uses several Azure services, such as storage, a secrets store, and messaging. Topaz provides one local environment in which to exercise the supported parts of that workflow without first provisioning each dependency in Azure.

::: {.scenario-diagram role="img" aria-label="A feature or group of microservices connects to Topaz and uses local storage, secrets, and messaging services."}
[Feature or microservices]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Topaz]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Storage, secrets, and messaging]{.scenario-node}
:::

### Scenario 5: Develop AI agents that use Azure services

Test an agent's calls to Azure-backed tools against local resources. For example, an agent can read a blob, retrieve a secret, or send a message through an emulated service. Use test data and verify that each required operation is supported.

::: {.scenario-diagram role="img" aria-label="An AI agent makes tool calls to Azure services running in Topaz."}
[AI agent]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Tool calls]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Topaz services]{.scenario-node}
:::

### Scenario 6: Test authorization and failure handling

Exercise supported authorization behavior and check how an application handles errors and retries. Where available, Topaz's fault-injection features can help test responses such as throttling, service unavailability, or timeouts without inducing those failures in a live Azure resource.

::: {.scenario-diagram role="img" aria-label="A test client exercises authorization and injected failures in Topaz, then verifies the application's response."}
[Test client]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Topaz authorization and fault injection]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Verify application response]{.scenario-node}
:::

### Scenario 7: Train and onboard developers

Give developers a local environment for exploring Azure SDK calls, resource creation, and application workflows. They can repeat exercises without sharing a development subscription or affecting shared Azure resources.

::: {.scenario-diagram role="img" aria-label="Developers use a local Topaz environment to practice SDK and resource-management workflows."}
[Developer]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Local Topaz environment]{.scenario-node} [&rarr;]{.scenario-arrow aria-hidden="true"} [Practice SDK and resource workflows]{.scenario-node}
:::

## How to use this book

The first chapters introduce Topaz and establish the prerequisites for running it. Later chapters will use that foundation to configure a local environment and connect development tools and application code.

The examples are intended for local development and testing. Keep local configuration separate from production configuration, and use credentials and test data appropriate for a local environment. Where an example depends on a service feature, verify that feature in the current coverage documentation for the Topaz version in use.
