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

Check the `start` command reference for the full list of supported options.

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