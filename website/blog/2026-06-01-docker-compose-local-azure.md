---
slug: docker-compose-local-azure-development
title: "Local Azure development with Docker Compose: a copy-paste starting point"
description: A practical guide to running Topaz alongside your application in Docker Compose for local Azure development. Covers fixed-IP networking, certificate distribution without bind mounts, startup ordering, and in-process ARM provisioning.
keywords: [docker compose azure local, topaz docker compose, local azure development docker, azure emulator docker compose, azurite alternative docker compose, azure sdk local development container]
authors: kamilmrzyglod
tags: [general, docker]
---

I had a simple target in mind: open a project, run `docker compose up`, and have a working local Azure environment — Key Vault, Blob Storage, ARM API. No manual steps on the host machine, no `az login`, no cloud subscription. Additionally, I wanted to avoid those pesky manual changes in the `hosts` file like `echo 127.0.0.1 topaz.local.dev >> /etc/hosts`, that someone will inevitably skip.

It sounds like a two-hour job. It took longer because three things that should be straightforward each had a non-obvious edge case.

{/* truncate */}

:::tip[Try it yourself]
The full example from this post is in the Topaz repository under [`Examples/Compose/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Compose). Run `./setup.sh && docker compose up` to start.

```bash
brew tap thecloudtheory/topaz && brew install topaz   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Docker Compose docs →](https://topaz.thecloudtheory.com/docs/ecosystem/docker-compose) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## What the setup actually needs to do

Before going into how things are connected, it helps to be explicit about what the setup has to achieve. Topaz routes Azure SDK clients by hostname, not by port alone. Key Vault calls go to `kv-my-app.vault.topaz.local.dev:8898`, Blob Storage calls go to `stmyapp001.blob.storage.topaz.local.dev:8891`. This is the same pattern as real Azure, where `kv-my-app.vault.azure.net` and `stmyapp001.blob.core.windows.net` are the hostnames your code actually connects to.

In Docker Compose, there is no automatic wildcard DNS for `*.topaz.local.dev`. The app container does not know where to send those requests unless you tell it explicitly. On a developer machine, Topaz configures `dnsmasq` via installation scripts to handle this. Inside a Docker network, you have two options.

The simplest approach is to use `extra_hosts`: map each Topaz hostname to a fixed IP assigned to the Topaz container. It requires no extra service and works immediately. The downside is that every new resource needs a new line in the Compose file. This unfortunately defeats the purpose and goes against the fundamental rule of Topaz - to require as few changes from a user as possible. The cleaner option, and the one the [Topaz devcontainer](/blog/devcontainer-topaz) uses, is a `dnsmasq` sidecar: a lightweight container that handles the `*.topaz.local.dev` wildcard automatically, so the app container never needs to be updated when you add resources.

Before we go any further, I'd like to highlight that I'm not against using `extra_hosts` with Topaz - it's a simple and quick solution to the problem of resolving custom DNS names. In static scenarios, when you're not provisioning infrastructure dynamically, this will work just fine and may be even better than spinning up the whole sidecar with `dnsmasq`. Conceptually though, I consider it a workaround.

Both options are covered below. The `Examples/Compose/` example in the repository uses `extra_hosts` for simplicity; the devcontainer uses the sidecar.

## The Compose file structure

Four top-level components:

1. A private bridge network with a fixed subnet, so the Topaz container gets a stable IP.
2. A named volume for the TLS certificate, populated before the containers start.
3. The `topaz` service.
4. Your `app` service, with DNS wired either via `extra_hosts` or through the sidecar.

The network definition:

```yaml
networks:
  topaz-net:
    driver: bridge
    ipam:
      config:
        - subnet: "172.28.0.0/16"
```

The Topaz service:

```yaml
topaz:
  image: thecloudtheory/topaz-host:latest
  platform: linux/amd64   # required on Apple Silicon
  networks:
    topaz-net:
      ipv4_address: "172.28.0.10"
  ports:
    - "8899:8899"   # ARM / Resource Manager
    - "8898:8898"   # Key Vault
    - "8891:8891"   # Blob Storage
    - "8889:8889"   # Service Bus (AMQP)
  volumes:
    - topaz-certs:/certs:ro
    - topaz-data:/app/.topaz
  command:
    - --certificate-file
    - /certs/topaz.crt
    - --certificate-key
    - /certs/topaz.key
    - --log-level
    - Information
```

The app service, with `extra_hosts` mapping each Topaz hostname to the fixed IP:

```yaml
app:
  build:
    context: .
    dockerfile: app/Dockerfile
  depends_on:
    - topaz
  networks:
    - topaz-net
  environment:
    AZURE_TENANT_ID: "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
  extra_hosts:
    - "topaz.local.dev:172.28.0.10"
    - "kv-my-app.vault.topaz.local.dev:172.28.0.10"
    - "stmyapp001.blob.storage.topaz.local.dev:172.28.0.10"
    - "sbnamespace.servicebus.topaz.local.dev:172.28.0.10"
```

Each Azure resource needs a corresponding `extra_hosts` entry. If you add a Container Registry named `myregistry`, add `myregistry.cr.topaz.local.dev:172.28.0.10`. Omitting an entry causes the SDK call to fail with a DNS resolution error, which is not always obvious to diagnose.

For stacks where enumerating every resource by name in the Compose file is too cumbersome, the sidecar approach handles this in one place. A minimal Alpine container installs `dnsmasq` at startup and registers a single wildcard rule pointing `*.topaz.local.dev` at the fixed Topaz IP:

```yaml
dns-sidecar:
  image: alpine:latest
  command: >
    sh -c "apk add --no-cache dnsmasq -q &&
           echo 'address=/.topaz.local.dev/172.28.0.10' > /etc/dnsmasq.d/topaz.conf &&
           dnsmasq --no-daemon --server=1.1.1.1 --server=8.8.8.8"
  networks:
    topaz-net:
      ipv4_address: "172.28.0.53"
  restart: unless-stopped
```

The app service then points its DNS resolver at the sidecar rather than using `extra_hosts`:

```yaml
app:
  dns:
    - 172.28.0.53   # sidecar resolves *.topaz.local.dev
    - 1.1.1.1       # fallback for internet hostnames
  depends_on:
    - dns-sidecar
    - topaz
```

With this in place, any `*.topaz.local.dev` hostname the app tries to resolve gets answered with `172.28.0.10`, regardless of what resources Topaz has provisioned. You can add a new Key Vault, a second Storage Account, or a Container Registry without touching the Compose file.

## The certificate problem

Topaz requires a TLS certificate to start. The certificate files ship with the repository and with every GitHub Release, so generating them is not the problem. Getting them into the container reliably is.

The obvious approach is a bind mount:

```yaml
topaz:
  volumes:
    - ./topaz.crt:/certs/topaz.crt:ro
    - ./topaz.key:/certs/topaz.key:ro
```

This breaks on Colima and on Docker Desktop when the project lives on an external drive or a path outside the configured file-sharing list. Docker Desktop on macOS silently creates an empty directory at the bind-mount target instead of failing with a useful error. You then see Topaz fail to start with a certificate file not found error, and the bind mount looks fine in `docker inspect`. Note though, that this may be related to my specific setup for local development rather than a general issue. Not everyone uses external drives on macOS and not everyone depends on Colima rather than Docker Desktop.

The [devcontainer post](/blog/devcontainer-topaz) hit the same problem. The fix there and here is the same: use `docker cp` to populate a named volume before any container starts. Named volumes are always accessible to containers regardless of host path permissions.

A short setup script handles this:

```bash
#!/bin/sh
set -e

VOLUME="topaz-certs"
docker volume create "$VOLUME" > /dev/null

CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp topaz.crt "$CONTAINER:/certs/topaz.crt"
docker cp topaz.key "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER" > /dev/null

echo "Done. Run 'docker compose up' to start the stack."
```

The volume is declared as `external: true` in the Compose file:

```yaml
volumes:
  topaz-certs:
    external: true
```

`external: true` tells Compose not to create or destroy this volume automatically. It survives `docker compose down`, which means `setup.sh` only needs to run once per machine, not on every restart.

## Startup ordering

`depends_on: topaz` starts the Topaz container before the app container, but it does not wait for Topaz to finish initialising. Topaz takes a second or two to load state and begin accepting connections. If the app tries to call the ARM API immediately at startup, the request fails.

The standard Compose fix for this is a healthcheck on the `topaz` service with `depends_on: condition: service_healthy` in the `app` service. That runs a command inside the Topaz container to test readiness. The Topaz image is a minimal .NET runtime image without `curl`, `bash`, or `nc`, so any shell-based healthcheck will fail immediately with a 127 exit code.

The approach that works is to have the application itself handle the retry. `TopazArmClient.CheckIfReadyAsync()` (C#) and `TopazArmClient.check_ready()` (Python) both call the unauthenticated `GET /health` endpoint on port 8899 and return `true` / `True` when they get a 200. Call them in a loop until Topaz is up:

```csharp
using var topazClient = new TopazArmClient(credential);
for (var attempt = 1; ; attempt++)
{
    if (await topazClient.CheckIfReadyAsync()) break;
    if (attempt >= 20) throw new TimeoutException("Topaz did not become ready after 40 seconds.");
    Console.WriteLine($"[startup] Topaz not ready yet (attempt {attempt}/20), retrying in 2 s...");
    await Task.Delay(TimeSpan.FromSeconds(2));
}
```

```python
import time
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID
from topaz_sdk.client import TopazArmClient

credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
client = TopazArmClient(credential)

for attempt in range(1, 21):
    if client.check_ready():
        break
    if attempt == 20:
        raise TimeoutError("Topaz did not become ready after 40 seconds.")
    print(f"[startup] Topaz not ready yet (attempt {attempt}/20), retrying in 2 s...")
    time.sleep(2)
```

This is a reasonable place for retry logic anyway. In production, you would retry connections to Azure services - it's one of the most common patterns when developing applications dependent on cloud components. Doing the same locally makes the behaviour consistent and tests a code path that matters.

## Provisioning resources at startup

Once Topaz is ready, the app provisions whatever Azure resources it needs using the ARM SDK. Because `CreateOrUpdateAsync` is idempotent, this is safe to run on every startup, including restarts against a volume that already has state from a previous run.

```csharp
var credential = new AzureLocalCredential("00000000-0000-0000-0000-000000000000");
var subscriptionId = "00000000-0000-0000-0000-000000000001";

await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "dev-local");

var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
var subscription = armClient.GetSubscriptionResource(
    SubscriptionResource.CreateResourceIdentifier(subscriptionId));

var rgResponse = await subscription.GetResourceGroups().CreateOrUpdateAsync(
    WaitUntil.Completed, "rg-my-app",
    new ResourceGroupData(AzureLocation.WestEurope));
var resourceGroup = rgResponse.Value;

await resourceGroup.GetKeyVaults().CreateOrUpdateAsync(
    WaitUntil.Completed, "kv-my-app",
    new KeyVaultCreateOrUpdateContent(
        AzureLocation.WestEurope,
        new KeyVaultProperties(Guid.Empty,
            new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(
    WaitUntil.Completed, "stmyapp001",
    new StorageAccountCreateOrUpdateContent(
        new StorageSku(StorageSkuName.StandardLrs),
        StorageKind.StorageV2,
        AzureLocation.WestEurope));

// Service Bus namespace and queue
var sbNamespaceResponse = await resourceGroup.GetServiceBusNamespaces().CreateOrUpdateAsync(
    WaitUntil.Completed,
    "sbnamespace",
    new ServiceBusNamespaceData(AzureLocation.WestEurope));

await sbNamespaceResponse.Value.GetServiceBusQueues().CreateOrUpdateAsync(
    WaitUntil.Completed,
    "sbqueue",
    new ServiceBusQueueData());
```

```python
from azure.mgmt.keyvault import KeyVaultManagementClient
from azure.mgmt.keyvault.models import VaultCreateOrUpdateParameters, VaultProperties, Sku as KVSku, SkuName as KVSkuName, SkuFamily
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import StorageAccountCreateParameters, Sku as StorageSku, SkuName as StorageSkuName, Kind
from azure.mgmt.servicebus import ServiceBusManagementClient
from azure.mgmt.servicebus.models import SBNamespace, SBSku, SkuName as SBSkuName, SkuTier, SBQueue
from azure.mgmt.resource import ResourceManagementClient
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID
from topaz_sdk.client import TopazArmClient
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT

SUBSCRIPTION_ID = "00000000-0000-0000-0000-000000000001"
RESOURCE_GROUP = "rg-my-app"
ARM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
ARM_SCOPES = [f"{ARM_BASE_URL}/.default"]

credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
topaz_client = TopazArmClient(credential)
topaz_client.create_subscription(SUBSCRIPTION_ID, "dev-local")

# Resource group
rmc = ResourceManagementClient(
    credential=credential,
    subscription_id=SUBSCRIPTION_ID,
    base_url=ARM_BASE_URL,
    credential_scopes=ARM_SCOPES,
)
rmc.resource_groups.create_or_update(RESOURCE_GROUP, {"location": "westeurope"})

# Key Vault
kvc = KeyVaultManagementClient(
    credential=credential, subscription_id=SUBSCRIPTION_ID,
    base_url=ARM_BASE_URL, credential_scopes=ARM_SCOPES,
)
kvc.vaults.begin_create_or_update(
    RESOURCE_GROUP, "kv-my-app",
    VaultCreateOrUpdateParameters(
        location="westeurope",
        properties=VaultProperties(
            tenant_id="50717675-3E5E-4A1E-8CB5-C62D8BE8CA48",
            sku=KVSku(family=SkuFamily.A, name=KVSkuName.STANDARD),
        ),
    ),
).result()

# Storage Account
stc = StorageManagementClient(
    credential=credential, subscription_id=SUBSCRIPTION_ID,
    base_url=ARM_BASE_URL, credential_scopes=ARM_SCOPES,
)
stc.storage_accounts.begin_create(
    RESOURCE_GROUP, "stmyapp001",
    StorageAccountCreateParameters(
        sku=StorageSku(name=StorageSkuName.STANDARD_LRS),
        kind=Kind.STORAGE_V2,
        location="westeurope",
    ),
).result()

# Service Bus namespace and queue
sbc = ServiceBusManagementClient(
    credential=credential, subscription_id=SUBSCRIPTION_ID,
    base_url=ARM_BASE_URL, credential_scopes=ARM_SCOPES,
)
sbc.namespaces.begin_create_or_update(
    RESOURCE_GROUP, "sbnamespace",
    SBNamespace(location="westeurope", sku=SBSku(name=SBSkuName.STANDARD, tier=SkuTier.STANDARD)),
).result()
sbc.queues.create_or_update(RESOURCE_GROUP, "sbnamespace", "sbqueue", SBQueue())
```

`AzureLocalCredential` is a credential class from the `Topaz.Identity` NuGet package. It issues tokens accepted by Topaz's local Entra ID emulation. The object ID `00000000-0000-0000-0000-000000000000` is Topaz's built-in superadmin identity, which has unrestricted access to all emulated resources. In practice you would use a specific object ID matched to whatever role assignments your application depends on, which exercises the RBAC path the same way it works in production. This is also supported in Topaz via emulated Entra ID APIs. For the sake of simplicity though, I use the superadmin account in the example.

## Connecting the SDK clients

After provisioning, building SDK clients is the same as usual, just pointing at Topaz endpoints. `TopazResourceHelpers` handles constructing the correct URIs:

```csharp
// Key Vault
var kvEndpoint = TopazResourceHelpers.GetKeyVaultEndpoint("kv-my-app");
var secretClient = new SecretClient(kvEndpoint, credential);

// Blob Storage — Shared Key via connection string
var storageAccount = await resourceGroup.GetStorageAccountAsync("stmyapp001");
var keys = storageAccount.Value.GetKeys().ToArray();
var connectionString = TopazResourceHelpers.GetAzureStorageConnectionString(
    "stmyapp001", keys[0].Value);
var blobServiceClient = new BlobServiceClient(connectionString);
```

```python
from azure.keyvault.secrets import SecretClient
from azure.storage.blob import BlobServiceClient as AzureBlobServiceClient
from topaz_sdk.helpers import TopazResourceHelpers

# Key Vault
kv_endpoint = TopazResourceHelpers.get_key_vault_endpoint("kv-my-app")
secret_client = SecretClient(vault_url=kv_endpoint, credential=credential)

# Blob Storage — Shared Key via connection string
storage_keys = stc.storage_accounts.list_keys(RESOURCE_GROUP, "stmyapp001")
connection_string = TopazResourceHelpers.get_storage_connection_string(
    "stmyapp001", storage_keys.keys[0].value
)
blob_service_client = AzureBlobServiceClient.from_connection_string(connection_string)
```

For Blob Storage, you can also authenticate with a token credential if your application uses managed identity in production:

```csharp
var serviceUri = new Uri(TopazResourceHelpers.GetBlobServiceUri("stmyapp001"));
var blobServiceClient = new BlobServiceClient(serviceUri, credential);
```

```python
blob_service_uri = TopazResourceHelpers.get_blob_service_uri("stmyapp001")
blob_service_client = AzureBlobServiceClient(account_url=blob_service_uri, credential=credential)
```

The token credential path is worth using during local development even if it is slightly more demanding setup-wise, because it exercises the RBAC check on every SDK call. A missing role assignment that would fail in production will fail locally too.

### Service Bus

For the Service Bus consumer, the plain Azure SDK path uses port 8889 with `UseDevelopmentEmulator=true`. `TopazResourceHelpers.GetServiceBusConnectionString` builds the connection string:

```csharp
var sbConnectionString = TopazResourceHelpers.GetServiceBusConnectionString("sbnamespace");
var serviceBusClient = new ServiceBusClient(sbConnectionString);

// Send a message
await using var sender = serviceBusClient.CreateSender("sbqueue");
await sender.SendMessageAsync(new ServiceBusMessage("hello from Compose"));

// Receive messages — ServiceBusProcessor handles the receive loop
var processor = serviceBusClient.CreateProcessor("sbqueue");
processor.ProcessMessageAsync += async args =>
{
    Console.WriteLine($"Received: {args.Message.Body}");
    await args.CompleteMessageAsync(args.Message);
};
processor.ProcessErrorAsync += args =>
{
    Console.Error.WriteLine(args.Exception);
    return Task.CompletedTask;
};
await processor.StartProcessingAsync();
```

```python
from azure.servicebus import ServiceBusClient, ServiceBusMessage
from topaz_sdk.helpers import TopazResourceHelpers

sb_connection_string = TopazResourceHelpers.get_service_bus_connection_string("sbnamespace")

with ServiceBusClient.from_connection_string(sb_connection_string) as sb_client:
    # Send a message
    with sb_client.get_queue_sender("sbqueue") as sender:
        sender.send_messages(ServiceBusMessage("hello from Compose"))

    # Receive messages — complete each message to remove it from the queue
    with sb_client.get_queue_receiver("sbqueue") as receiver:
        for msg in receiver:
            print(f"Received: {msg}")
            receiver.complete_message(msg)
```

This is the SDK's receive-and-delete / peek-lock path with `UseDevelopmentEmulator=true`. If you are using MassTransit or another framework that manages its own PeekLock cycle, use `GetServiceBusConnectionStringWithTls` (port 5671) and expose port `5671` in the Compose file instead. The [AMQP post](/blog/amqp-compatibility-local-azure-emulator) covers why that distinction matters. It's also important to highlight here that Topaz doesn't require you to use `UseDevelopmentEmulator=true` in the connection string and, in fact, rather discourages that practice. Some SDKs behave differently if they see an emulator in the connection string, so following that path may hide bugs that would surface when using a real connection string.

## TLS trust in the application container

The Azure SDK clients will refuse Topaz's self-signed certificate by default. The certificate needs to be trusted by the OS inside the application container.

For .NET containers based on Debian or Ubuntu:

```dockerfile
COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates
```

For other runtimes:

```yaml
environment:
  SSL_CERT_FILE: "/certs/topaz.crt"      # Python, Go, Terraform
  NODE_EXTRA_CA_CERTS: "/certs/topaz.crt" # Node.js
```

If you mount the `topaz-certs` volume into the app container as well, you can reference `/certs/topaz.crt` without baking the certificate into the image.

## What the running stack looks like

With `setup.sh` run once and `docker compose up` started, the app container boots, waits for Topaz to be ready, provisions the resource group, Key Vault, and storage account, then starts accepting traffic. A `GET /health` to the app returns the Key Vault endpoint and blob endpoint it configured:

```
[startup] Topaz not ready yet (attempt 1/20), retrying in 2 s...
[startup] Topaz ready.
[startup] Creating subscription 00000000-0000-0000-0000-000000000001...
[startup] Creating resource group rg-my-app...
[startup] Creating Key Vault kv-my-app...
[startup] Creating storage account stmyapp001...
[startup] Done. Listening on http://+:8080.
```

From there, `curl http://localhost:8080/secrets/mypassword` hits the emulated Key Vault. `curl -X PUT http://localhost:8080/blobs/uploads/test.txt -d "hello"` writes to emulated Blob Storage. The app has no idea it is not talking to Azure.

## What does not work yet

Topaz is still in development and some features are not there yet. Dead-letter queues, message sessions, and Cosmos DB emulation are not available in the latest release but will be added soon. If your application depends on those, the current Compose setup only gets you partway. The [roadmap](https://topaz.thecloudtheory.com/roadmap) tracks when those are coming.

When following this tutorial, remember that state persists in the `topaz-data` volume. `docker compose down` leaves it intact. `docker compose down -v` removes it and starts fresh on the next `docker compose up`, which is useful for test runs where you want a clean environment each time. Keep in mind `down -v` will not remove `topaz-certs` because it is declared as external — that volume stays until you delete it manually.
