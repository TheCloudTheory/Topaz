# Topaz on Kubernetes — shared emulator for multiple applications

This example shows how to run Topaz as a **self-managed, shared service** inside a
local Kubernetes cluster so that multiple application teams can use the same emulator
without each team running their own instance.

The cluster is created with **k3d**, which runs k3s inside Docker containers and works
on macOS, Linux, and Windows with Docker Desktop. CoreDNS is patched so that every
`*.topaz.local.dev` hostname (used by Topaz data-plane SDKs) resolves to the Topaz
`Service` — no `extra_hosts` or environment variable overrides are needed in the apps.

```
┌──────────────── k3d cluster ─────────────────────────────┐
│                                                          │
│  ┌─────────────────────────────┐                         │
│  │  topaz-system namespace     │                         │
│  │  ┌─────────────────────┐    │                         │
│  │  │  Topaz Pod          │    │                         │
│  │  │  (shared emulator)  │◄───┼──────────────────┐      │
│  │  └─────────────────────┘    │                  │      │
│  └─────────────────────────────┘                  │      │
│                                                   │      │
│  ┌─────────────────────────────┐                  │      │
│  │  apps namespace             │  CoreDNS rewrite │      │
│  │  ┌──────────────────┐       │  *.topaz.local   │      │
│  │  │  order-service   │───────┼──.dev → topaz    │      │
│  │  └──────────────────┘       │    .topaz-system │      │
│  │  ┌──────────────────┐       │    .svc.cluster  │      │
│  │  │  inventory-svc   │───────┼──.local          │      │
│  │  └──────────────────┘       │                  │      │
│  └─────────────────────────────┘                  │      │
└───────────────────────────────────────────────────┘      │
```

## Prerequisites

| Tool | Install |
|---|---|
| Docker | https://docs.docker.com/get-docker/ |
| k3d | `brew install k3d` or https://k3d.io |
| kubectl | `brew install kubectl` |
| .NET 10 SDK | https://dot.net |

## Quick start

```sh
chmod +x setup.sh
./setup.sh
```

The script:
1. Copies the Topaz TLS certificate from `../../certificate/`
2. Creates a k3d cluster named `topaz-demo`
3. Patches CoreDNS to resolve `topaz.local.dev` and `*.topaz.local.dev` to the Topaz service
4. Deploys Topaz in the `topaz-system` namespace
5. Builds and imports the two sample application images
6. Deploys `order-service` and `inventory-service` in the `apps` namespace

## Verifying

```sh
# Watch all pods come up
kubectl get pods -A -w

# Check order-service logs (shows ARM provisioning + blob operations)
kubectl logs -n apps -l app=order-service -f

# Check inventory-service logs (shows ARM provisioning + queue operations)
kubectl logs -n apps -l app=inventory-service -f

# Hit the order-service HTTP endpoint
kubectl port-forward -n apps svc/order-service 8081:80 &
curl -X PUT http://localhost:8081/orders/order-1 -d '{"item":"widget","qty":3}'
curl -X PUT http://localhost:8081/orders/order-2 -d '{"item":"gadget","qty":1}'
curl http://localhost:8081/orders
curl http://localhost:8081/orders/order-1

# Hit the inventory-service HTTP endpoint
kubectl port-forward -n apps svc/inventory-service 8082:80 &
curl -X POST http://localhost:8082/items -d '{"sku":"widget","stock":100}'
curl -X POST http://localhost:8082/items -d '{"sku":"gadget","stock":42}'
curl http://localhost:8082/items
```

## How DNS integration works

Topaz data-plane endpoints use subdomains of `topaz.local.dev`, for example:
- `orders-store.blob.storage.topaz.local.dev` (Blob Storage)
- `kv-orders.vault.topaz.local.dev` (Key Vault)

`setup.sh` patches the CoreDNS `ConfigMap` in `kube-system` to add a rewrite rule
that maps any `*.topaz.local.dev` query to `topaz.topaz-system.svc.cluster.local`.
This means application code and SDK clients work identically inside the cluster as
they would with a real Azure endpoint — no custom connection strings required.

## Namespace isolation

Each application team gets its own Azure subscription and resource group inside Topaz:

| App | Subscription | Resource group |
|---|---|---|
| order-service | `00000000-0000-0000-0000-000000000001` | `rg-orders` |
| inventory-service | `00000000-0000-0000-0000-000000000002` | `rg-inventory` |

Resources are fully isolated at the Topaz level even though both apps share a single
emulator process.

## Tear down

```sh
k3d cluster delete topaz-demo
```
