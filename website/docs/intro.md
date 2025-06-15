---
sidebar_position: 1
---

# Getting started

Let's discover what is Topaz and how you may benefit from it.

## What is Topaz?

Topaz is an Azure emulator, which allows you to develop Azure-based applications without a need to connect to cloud services. It mimics popular Azure components such as Azure Storage, Azure Key Vault or Azure Service Bus to provide a robust local environment.

### What you'll need

Topaz is distributed as a self-container binary meaning the only thing you need is downloading the selected release package from the [releases page](https://github.com/TheCloudTheory/Topaz/releases). However, if you prefer to run as a containerized service, you will need a container runtime such as Docker.

## How to install Topaz?

Topaz doesn't require installation and can be run as either a single executable or a Docker container. If you want to run it as a standalone application, make sure you've installed and trusted certificates (unless you don't need to use HTTPS endpoints). The certificates are attached to each release package. We strongly recommend running Topaz as a Docker container though as it saves you from complexity of local installation:

```bash
docker pull thecloudtheory/topaz-cli:<tag>
```

Image tags are always aligned with the Git tag linked to a specific release.

One of the best options to run Topaz is to leverage [Testcontainers](https://testcontainers.com/). Check the rest of the documentation for the detailed instructions.

## Start the emulator

Depending on the selected approach (standalone executable vs container), you will need different commands to start the emulator.

### Running the executable

```bash
# For Unix/Linux systems
cd <executable-download-directory>
chmod +x <executable-name>
./<executable-name> start --log-level Information

# For Windows
.\topaz-win-x64.exe start --log-level Information
```

Make sure you downloaded the correct binary depending on the architecture of your processor.

### Running Topaz as a container
```bash
docker run --rm -p 8899:8899 thecloudtheory/topaz-cli:<tag> start --log-level Information
```
As various emulated services are exposed using different ports, you need to explicitly tell Docker (or similar tool you're using for running containers) which ports should be available on the host machine.

Note that using `start` command isn't the only option available for Topaz. You can learn more about other commands on the next page.
