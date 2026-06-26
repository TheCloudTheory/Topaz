---
sidebar_position: 6
description: How Topaz's App Service forward proxy works — transparent HTTP forwarding from a *.azurewebsites.topaz.local.dev hostname to your local container, and why this is the foundation for advanced Docker Compose scenarios.
keywords: [topaz app service proxy, azure app service local, forward proxy docker compose, topaz forward proxy, local app service emulator, azure app service emulator, docker compose azure app service]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# App Service forward proxy

Topaz includes a transparent HTTP forward proxy that makes any container reachable at a real Azure App Service hostname. This is the foundation for Docker Compose setups where your application calls other services _as if_ they were deployed to Azure App Service — without changing a single line of application code.

## The problem it solves

When code uses the Azure App Service hosting model, it addresses other services by their `*.azurewebsites.net` hostname, not by a Docker service name or IP address. Replacing those hostnames during local development normally requires environment-specific configuration or mock wrappers.

The forward proxy eliminates that need. Topaz intercepts HTTPS traffic arriving on port **8900** for any `*.azurewebsites.topaz.local.dev` hostname and forwards it, verbatim, to the container whose name matches the site name.

## How it works

```
Client
  │  HTTPS GET https://backend.azurewebsites.topaz.local.dev:8900/api/hello
  ▼
Topaz (port 8900)
  │  1. Extract site name from Host header → "backend"
  │  2. Look up App Service site resource in control plane
  │  3. Read WEBSITES_PORT app setting (default: 80)
  │  4. Build target URL → http://backend:{WEBSITES_PORT}/api/hello
  │  5. Forward request (method, headers, body)
  ▼
Backend container  →  response streamed back to client
```

### Step 1 — Site name extraction

The proxy reads the `Host` header from the incoming request:

```
backend.azurewebsites.topaz.local.dev:8900
└──────┘
  site name = "backend"
```

### Step 2 — Control plane lookup

Topaz looks up the site in its App Service control plane. If no site with that name has been provisioned, the proxy returns **404**. This mirrors real Azure behaviour: a request to a non-existent App Service site is rejected at the platform level before any container is involved.

### Step 3 — Port resolution via `WEBSITES_PORT`

Azure App Service reads the `WEBSITES_PORT` app setting to know which port your container listens on. Topaz honours the same setting. If it is absent, port **80** is used as the default.

```bash
# Tell Topaz your container listens on port 3000
az webapp config appsettings set \
  -n backend -g my-rg \
  --settings WEBSITES_PORT=3000
```

### Step 4 — Target URL construction

The proxy builds the forwarding URL as:

```
http://{siteName}:{WEBSITES_PORT}{path}{query}
```

The **site name doubles as the Docker Compose service name**. Docker's internal DNS resolves the service name to the container IP on the shared network, so no additional configuration is required.

### Step 5 — Request forwarding

The proxy copies the original request method, all headers, and the request body to the target, then streams the response (status code, headers, body) back to the caller. Two headers are added automatically:

| Header | Value |
|---|---|
| `X-Forwarded-For` | Client IP address |
| `X-Forwarded-Host` | Original `Host` header value |

## Docker Compose setup

A minimal Compose file that wires up the forward proxy:

```yaml
services:
  topaz:
    image: topaz/host:latest
    ports:
      - "8899:8899"   # ARM / Resource Manager
      - "8900:8900"   # App Service forward proxy
    networks:
      topaz-net:
        ipv4_address: "172.28.0.10"

  # Compose service name must match the App Service site name in Topaz
  backend:
    image: my-backend-app:latest
    networks:
      - topaz-net
    environment:
      WEBSITES_PORT: "8080"   # port your app listens on

  app:
    image: my-caller-app:latest
    networks:
      - topaz-net
    extra_hosts:
      # Route *.azurewebsites.topaz.local.dev to Topaz
      - "backend.azurewebsites.topaz.local.dev:172.28.0.10"
```

The `extra_hosts` entry in the calling service routes the Azure App Service hostname to Topaz. Topaz then forwards it to the `backend` container over the Docker-internal network.

A working example with a one-shot provisioner is available at [`Examples/Compose/AppServiceForwardProxy/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Compose/AppServiceForwardProxy).

## Provisioning the site

The proxy returns **404** for any hostname whose site has not been registered. Provision the site once before traffic flows — via the Azure CLI, the Azure SDK, or the Topaz CLI:

<Tabs>
<TabItem value="cli" label="Azure CLI">

```bash
az group create -n my-rg -l westeurope
az appservice plan create -n my-plan -g my-rg --sku B1
az webapp create -n backend -g my-rg --plan my-plan
az webapp config appsettings set -n backend -g my-rg \
  --settings WEBSITES_PORT=8080
```

</TabItem>
<TabItem value="sdk" label=".NET SDK">

```csharp
var resourceGroup = await subscription.GetResourceGroups()
    .CreateOrUpdateAsync(WaitUntil.Completed, "my-rg",
        new ResourceGroupData(AzureLocation.WestEurope));

var plan = await resourceGroup.Value.GetAppServicePlans()
    .CreateOrUpdateAsync(WaitUntil.Completed, "my-plan",
        new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic" }
        });

var site = await resourceGroup.Value.GetWebSites()
    .CreateOrUpdateAsync(WaitUntil.Completed, "backend",
        new WebSiteData(AzureLocation.WestEurope) { Kind = "app" });

var settings = new AppServiceConfigurationDictionary();
settings.Properties.Add("WEBSITES_PORT", "8080");
await site.Value.UpdateApplicationSettingsAsync(settings);
```

</TabItem>
</Tabs>

## Foundation for advanced scenarios

The forward proxy is a prerequisite for emulating more complex Azure networking constructs locally. Once your containers are addressable via real App Service hostnames, you can layer on:

| Scenario | What it requires |
|---|---|
| **Load balancing** | Multiple containers mapped to different site names; a gateway container that fans out using the App Service URLs |
| **Network isolation** | Docker networks scoped per service family; `extra_hosts` limited to the services that should be reachable |
| **Network policies** | Firewall rules enforced by a sidecar or gateway that validates the `X-Forwarded-Host` header set by the proxy |
| **Blue/green deployments** | Two Compose service variants (`backend-blue`, `backend-green`) swapped by updating which name is registered as the active App Service site |
| **Circuit breaking and retries** | A proxy sidecar (e.g. Envoy) inserted between Topaz and the backend; App Service hostname routing remains unchanged |
| **Service mesh (local)** | Each service addressed only via its App Service hostname; sidecar proxies handle mTLS and telemetry collection |

Each of these builds on the same primitive: any container is reachable at a stable `*.azurewebsites.topaz.local.dev` hostname, independent of its Docker IP or internal port.

## Certificate requirement

The forward proxy listens on HTTPS. The Topaz wildcard certificate must cover `*.azurewebsites.topaz.local.dev` and be trusted by the OS or the calling tool. See [DNS and TLS](./dns-and-tls.md) for setup instructions.
