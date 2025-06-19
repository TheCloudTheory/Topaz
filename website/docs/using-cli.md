---
sidebar_position: 3
---

# Using Topaz CLI

Topaz comes in a form of a single tool, which combines the capabilities of an underlying emulator and CLI. Whether you downloaded the Topaz executable or pulled its container image, you have the choice of running it in one of the available modes.

## Running Topaz as emulator
If you downloaded Topaz as an executable, you need to explicitly provide the `start` command to run it as an emulator:
```bash
./topaz start --log-level Information
```

When running it as a container though, the `start` command is run by default. In other words this command:
```bash
docker run --rm -p 8899:8899 thecloudtheory/topaz-cli:<tag> start --log-level Information
```

can be simplified with the following one:
```bash
docker run --rm -p 8899:8899 thecloudtheory/topaz-cli:<tag>
```

Check the [`start`](./cli-reference/emulator/start.md) command reference for the full list of supported options.

### Bring-you-own-certificate (BYOC)
Topaz supports using a custom-provided certificate if you can't trust the one which is shipped with it. To configure Topaz to use your certificate, simply pass the location of both the certificate and the private key when starting the emulator:
```bash
$ topaz start \
    --certificate-file "/path/to/your/certificate.crt" \
    --certificate-key "/path/to/your/private.key"
```
:::warning

Currently BYOC feature won't work on macOS machines because Topaz doesn't support providing a custom PFX certificate yet. A workaround is to run the emulator as a container. See [this](https://github.com/TheCloudTheory/Topaz/issues/20) issue for more information.

:::

If you're running Topaz as a container, make sure you mounted the directory containing the certificate and the key when starting it:
```bash
docker run -d \
  --name thecloudtheory/topaz-cli:<tag> \
  -p 8899:8899 \
  -v /path/to/your/certificate.crt:/app/certificate.crt:ro \
  -v /path/to/your/private.key:/app/private.key:ro \
  start --certificate-file "certificate.crt" --certificate-key "private.key"
```

## Running Topaz CLI 
If Topaz is running in the background, you can leverage its CLI to interact with both control and data plane of emulated services. For instance, you could run the following 2 commands to create a new subscription and a resource group:
```bash
./topaz subscription create --id <subscription-id> --name <subscription-name>
./topaz group create --name <resource-group-name> --location <location> --subscription-id <subscription-id>
```

You can always check the available commands with their parameters and options by running:
```bash
./topaz -h
```