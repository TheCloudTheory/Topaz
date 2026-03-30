---
sidebar_position: 7
description: Troubleshooting guide for common Topaz issues — TLS certificate errors, DNS resolution failures, Azure SDK connection problems, AMQP connectivity, and more.
keywords: [topaz troubleshooting, azure emulator errors, tls certificate error, dns resolution azure local, azurite alternative issues]
---

# Troubleshooting

This page collects the most common issues encountered when running, configuring, and integrating Topaz. Each section targets a specific problem area. If you run into something not listed here, check the Topaz process output — every error is logged with context.

## Port reference

These are the default ports Topaz binds to. Use the table whenever you suspect a port conflict or need to open firewall rules.

| Service | Port | Protocol | Notes |
|---|---|---|---|
| Resource Manager / Entra / general | 8899 | HTTPS | Primary control-plane endpoint |
| Key Vault | 8898 | HTTPS | |
| Event Hub HTTP | 8897 | HTTPS | |
| Service Bus AMQP | 8889 | AMQP | |
| Service Bus additional | 8887 | HTTPS | |
| Event Hub AMQP | 8888 | AMQP | |
| Table Storage | 8890 | HTTPS | |
| Blob Storage | 8891 | HTTPS | |
| AMQP over TLS | 5671 | AMQP/TLS | Enabled when a certificate is provided |

Check which processes are listening on a port:

```bash
# macOS / Linux
lsof -i :<port>
# or
ss -tlnp | grep <port>
```

---

## Installation & first run

### DNS resolution fails (`*.topaz.local.dev` not found)

Topaz relies on wildcard DNS to route service-specific hostnames (e.g. `myvault.vault.topaz.local.dev`) to localhost.

**macOS** — run the bundled install script, then restart dnsmasq:
```bash
./install/install-macos.sh
brew services restart dnsmasq
```

Verify resolution:
```bash
dig test.topaz.local.dev @127.0.0.1
scutil --dns | grep topaz
```

**Linux** — run as root (handles the `systemd-resolved` conflict automatically):
```bash
sudo ./install/install-linux.sh
```

If port 53 is already in use by `systemd-resolved`, the script stops and disables it before installing dnsmasq. Confirm with:
```bash
systemctl status dnsmasq
dig test.topaz.local.dev @127.0.0.1
```

**WSL** — DNS resolution inside WSL uses the Windows host's resolver. Run the Windows install script, then add the following to `/etc/wsl.conf` to prevent WSL from overwriting `/etc/resolv.conf`:
```ini
[network]
generateResolvConf = false
```

Restart WSL (`wsl --shutdown`) and verify with `nslookup test.topaz.local.dev`.

---

### `CERTIFICATE_VERIFY_FAILED` / TLS errors

Topaz uses a self-signed certificate. TLS clients that validate certificates against the system trust store will fail until the root CA is imported.

**Re-run the certificate configuration script:**
```bash
# Azure CLI
./install/configure-azure-cli-cert.sh

# macOS system trust store
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain topaz.crt

# Ubuntu / Debian
sudo cp topaz.crt /usr/local/share/ca-certificates/topaz.crt
sudo update-ca-certificates

# RHEL / Fedora / CentOS
sudo cp topaz.crt /etc/pki/ca-trust/source/anchors/topaz.crt
sudo update-ca-trust
```

**For .NET clients** — trust the certificate at the OS level (above) or construct the `HttpClient` with certificate validation disabled in local/test environments:
```csharp
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};
```

**For the Azure SDK** — pass `TopazArmClientOptions` when constructing the `ArmClient`:
```csharp
var client = new ArmClient(credential, subscriptionId, new TopazArmClientOptions());
```

`TopazArmClientOptions` (from `Topaz.AspNetCore.Extensions`) configures the SDK to bypass TLS validation and points it at the local endpoint automatically.

---

### Port already in use

If Topaz fails to start because a port is already bound:

```bash
# Find the process holding the port
lsof -i :8899
kill -9 <PID>
```

Or use the `--port` flag (if supported by the service) to start on an alternate port, and update any SDK/CLI configuration accordingly.

---

### `topaz.crt` or `topaz.key` not found

Topaz auto-generates a certificate on first run if neither file is found. If you want to supply your own:

```bash
topaz start --certificate-file ./my.crt --certificate-key ./my.key
```

The certificate files must be PEM-encoded. Use the bundled generation script to create a compatible self-signed pair:

```bash
./certificate/generate.sh
```

---

## Azure CLI integration

### `az` commands return 404 / `ResourceNotFound`

The wrong cloud is active. Confirm with:
```bash
az cloud show --query name
```

It should print `Topaz`. If it doesn't:
```bash
az cloud set --name Topaz
az login
```

### `az login` opens no browser / hangs

On WSL and headless Linux sessions, the device-code flow is more reliable:
```bash
az login --use-device-code
```

### `InteractionRequiredAuthError`

This happens when the Azure AD tenant has Conditional Access policies that block local authentication. Use a dedicated test tenant without Conditional Access, or a service principal with `az login --service-principal`.

### Subscription not found

If `az` returns errors about missing subscriptions, Topaz was started without a default subscription. Restart with:
```bash
topaz start --default-subscription 00000000-0000-0000-0000-000000000000
```

Or create the subscription manually after starting:
```bash
topaz subscription create --name "Default" --subscription-id 00000000-0000-0000-0000-000000000000
```

### Switching back to real Azure

```bash
az cloud set --name AzureCloud
az login
# Re-enable instance discovery if it was disabled
export AZURE_CORE_INSTANCE_DISCOVERY=true
```

---

## .NET SDK & application integration

### `AuthenticationFailedException` / 401 responses

Most authentication errors come from the credential chain not finding `AzureLocalCredential`.

1. Confirm you are using `AzureLocalCredential` (from `Topaz.Identity`) instead of `DefaultAzureCredential`.
2. Confirm the `ArmClient` is pointed at the local endpoint:
   ```csharp
   var client = new ArmClient(
       new AzureLocalCredential(),
       subscriptionId,
       new TopazArmClientOptions());
   ```
3. Check that Topaz is running and reachable: `curl -sk https://localhost:8899/subscriptions`

### `CredentialUnavailableException`

The `AzureLocalCredential` issues tokens signed for the local Entra endpoint. If you see this error, verify:
- Topaz is running on the expected port (8899 by default).
- The token endpoint in the credential is configured to point to `https://topaz.local.dev:8899`.
- No firewall rule is blocking port 8899.

### ASP.NET Core `AddTopaz()` throws at startup

The most common cause is a missing `objectId` argument. The correct call requires both a subscription ID and a service-principal object ID:
```csharp
builder.Services.AddTopaz(
    subscriptionId: "00000000-0000-0000-0000-000000000000",
    objectId: "00000000-0000-0000-0000-000000000001");
```

---

## Docker & containers

### Connection refused when running Topaz in Docker

| Symptom | Likely cause | Fix |
|---|---|---|
| `Connection refused` on any port | Container not ready | Add a readiness wait loop — see [CI/CD](ecosystem/ci-cd.md#using-the-docker-container-recommended) |
| `Connection refused` from another container | Wrong hostname | Use `host.docker.internal` (Docker Desktop) or `--network host` (Linux) |
| DNS not resolving inside container | `install-linux.sh` not run on host | Run installer on the host before starting containers |
| Port not reachable from tests | Port not exposed | Add `-p 8899:8899` (and other ports) to `docker run` |

### `--network host` not working on macOS / Windows

`--network host` only has effect on Linux. On macOS and Windows (Docker Desktop), use `host.docker.internal` as the hostname instead of `localhost` or `127.0.0.1`:

```csharp
var endpoint = new Uri("https://host.docker.internal:8899");
```

### Image not found / `manifest unknown`

Ensure you are using the correct image name and tag:
```bash
docker pull thecloudtheory/topaz-cli:latest
```

Check available tags at [Docker Hub](https://hub.docker.com/r/thecloudtheory/topaz-cli/tags).

---

## ARM template deployments

### Deployment succeeds but resource is not created

The resource type may not be supported. Topaz logs a warning and skips unknown types:

```
WARN  Deployment of Microsoft.X/y is not yet supported.
```

Check the [supported resource types table](ecosystem/arm-deployments.md#supported-resource-types) and remove unsupported types from the template, or file an issue to request support.

### Template expression evaluation errors

Topaz uses the same `Azure.Deployments` expression engine as Azure. If an expression fails:
- Check the Topaz log for the full error message.
- Verify the expression against the [ARM template reference](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions).
- Functions that call back into ARM (e.g. `listKeys`) may return empty results for resource types that are not fully emulated.

### Parameters file not applied

Ensure the parameters file follows the ARM schema:
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "myParam": { "value": "my-value" }
  }
}
```

Pass it with `--parameters @file.parameters.json` (Azure CLI) or via `ArmDeploymentProperties.Parameters` (SDK).

---

## Data persistence

### State is lost between runs

By default Topaz persists state to the `.topaz/` directory in the working directory where `topaz start` is run. Ensure you always run from the same directory, or specify a consistent path.

### Resetting all state

Delete the `.topaz/` directory to wipe all persisted resources and restart from a clean slate:
```bash
rm -rf .topaz/
topaz start
```

---

## Logs & diagnostics

### Enabling verbose logging

```bash
topaz start --verbosity debug
```

Verbose mode prints every request, response code, and resource operation to stdout.

### Finding the Topaz process log

When running as a background process or inside Docker, redirect stdout to a file:
```bash
# Docker
docker logs topaz

# Bare process
topaz start > topaz.log 2>&1 &
tail -f topaz.log
```

### Smoke-testing a running instance

```bash
# Check the emulator is responding
curl -sk https://localhost:8899/subscriptions | jq .

# List resource groups in a subscription
curl -sk "https://localhost:8899/subscriptions/00000000-0000-0000-0000-000000000000/resourcegroups" | jq .
```

A `200 OK` with a JSON body confirms the emulator is up. A connection error means Topaz is not running or not bound to that port.
