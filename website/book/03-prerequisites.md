# Prerequisites

Topaz runs locally, but the setup differs depending on whether you use a native executable or a container. Choose one method and complete the DNS and certificate steps for that method before connecting Azure tools or application clients.

## Operating system and runtime

Topaz provides native packages for macOS and Linux on supported processor architectures. On Windows, the recommended environment is Windows Subsystem for Linux 2 (WSL 2); the installation and shell commands in this book can be run inside that Linux environment.

The native Topaz binaries are self-contained. You do not need to install the .NET runtime just to run `topaz-host` or `topaz`. You do need the runtime and development tools required by your own application. If you choose the container option, install Docker or another compatible container runtime instead.

## Choose an installation method

### macOS with Homebrew

Install the Topaz package using Homebrew:

```bash
brew tap thecloudtheory/topaz
brew install topaz
```

This installation configures the Topaz binaries and DNS setup. You must still trust the Topaz certificate before HTTPS clients can connect.

### Linux or WSL 2

The install script downloads the Topaz binaries and adds them to your `PATH`:

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash
```

Run the one-time DNS setup script separately, as described below. If you prefer Homebrew on Linux, the Homebrew installation method is also available.

### Docker

For a container-based installation, you need a compatible container runtime and the Topaz host image, `thecloudtheory/topaz-host`. Publish the ports required by the Azure services used in your workflow. The [Docker guide](https://topaz.thecloudtheory.com/docs/ecosystem/docker-compose/) lists the current port configuration and deployment options.

Containerizing Topaz does not remove the need to configure how clients resolve Topaz service names. Follow the DNS and certificate instructions for the machine and client environment that will access the container.

## Configure DNS

Topaz uses local service hostnames, such as `*.topaz.local.dev`, to route requests to the correct service endpoint. A one-time setup script configures DNS resolution for those names. The script requires administrator privileges because it changes machine-level DNS configuration.

Use the script for your operating system from the [getting-started guide](https://topaz.thecloudtheory.com/docs/intro/). On Windows, run the Linux instructions inside the WSL 2 instance where Topaz is installed. DNS configuration is per machine or WSL instance; it is not repeated for each project.

## Trust the HTTPS certificate

Topaz uses a certificate for its HTTPS endpoints. Clients that do not trust this certificate will fail TLS validation, even when Topaz is running and DNS resolves correctly. Install and trust the certificate using the instructions for your operating system and installation method in the [getting-started guide](https://topaz.thecloudtheory.com/docs/intro/).

On macOS, developers using .NET-based clients may also need to trust the certificate in the login keychain. The getting-started guide documents this additional step. For Docker, the certificate is available from the image; the host or client container must trust it as appropriate for the connection path.

Do not disable TLS certificate validation as a workaround. Configure trust for the Topaz certificate in the environment that runs the client.

## Install tools for your examples

Topaz can be used with Azure SDKs and development tools that support its endpoints. Install only the tools required by your application and the examples you plan to follow. Depending on the workflow, these may include:

- A language runtime, SDK, and build tools for the application.
- The Azure CLI, if the workflow uses `az` commands.
- Terraform or Bicep tooling, if the workflow provisions resources from infrastructure definitions.
- Docker or a compatible container runtime, if Topaz or the application runs in containers.

These tools are optional for running the native Topaz host itself. An Azure subscription is not required for local development with Topaz. A real Azure subscription is still required to validate behavior against Azure or deploy resources there.

## Verify the setup

Start the emulator in a terminal:

```bash
topaz-host --log-level Information
```

In a second terminal, run:

```bash
topaz health
```

The command should report that the host is running. If it cannot connect, check that the host is still running, DNS setup is complete, and the required ports are available and published for your installation method. If HTTPS requests fail, verify certificate trust in the environment running the client.

Before beginning a service-specific exercise, check the [supported services](https://topaz.thecloudtheory.com/docs/supported-services/) and operation-level API coverage for the Topaz version you installed. Coverage can change between releases.
