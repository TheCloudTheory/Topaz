---
sidebar_position: 7
slug: /ecosystem/kubernetes
description: Deploy Topaz as a shared emulator inside a Kubernetes cluster, integrate with CoreDNS, and isolate multiple applications using separate Topaz subscriptions.
keywords: [topaz kubernetes, azure emulator kubernetes, topaz k8s, local azure kubernetes, coredns topaz, shared azure emulator]
---

# How to run Topaz in Kubernetes

This guide shows how to deploy Topaz as a **shared, self-managed emulator** inside a Kubernetes cluster so multiple applications can use the same instance without each team running their own copy.

## Concept

In a microservices setup it quickly becomes wasteful — and hard to keep in sync — for every developer or CI job to maintain a private Topaz instance. A better model is to treat Topaz the same way you treat any shared backing service: deploy it once, give each application team an isolated Azure **subscription** inside that shared instance, and let Kubernetes DNS take care of the routing.

![Topaz Kubernetes architecture — shared emulator with CoreDNS rewrite and per-app subscription isolation](/img/topaz-kubernetes-architecture.svg)

### Why CoreDNS instead of `extra_hosts`?

Docker Compose solves hostname resolution with `extra_hosts`. In Kubernetes, the equivalent would be adding a `hostAliases` entry to every Pod spec — which is fragile and doesn't scale.

A cleaner solution is to patch the cluster-wide CoreDNS `ConfigMap` with a single rewrite rule. Any query for `*.topaz.local.dev` is transparently redirected to the Topaz `ClusterIP` service. Application code and SDK clients require no changes — they use the same hostnames they would against real Azure.

## Namespace isolation

Each application gets its own Topaz subscription. Resources created inside one subscription are invisible to another, even though both share the same emulator process.

| App | Topaz subscription | Resource group |
|---|---|---|
| order-service | `00000000-0000-0000-0000-000000000001` | `rg-order-service` |
| inventory-service | `00000000-0000-0000-0000-000000000002` | `rg-inventory-service` |

Pass the subscription ID to each pod as an environment variable:

```yaml title="pod spec"
env:
  - name: TOPAZ_SUBSCRIPTION_ID
    value: "00000000-0000-0000-0000-000000000001"
```

## Kubernetes manifests

### Namespaces

```yaml title="namespaces.yaml"
apiVersion: v1
kind: Namespace
metadata:
  name: topaz-system
---
apiVersion: v1
kind: Namespace
metadata:
  name: apps
```

### Topaz deployment

Topaz runs as a single-replica `Deployment` in the `topaz-system` namespace. The TLS certificate is mounted from a `Secret` so pods can be recreated without losing it.

```yaml title="topaz-deployment.yaml"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: topaz
  namespace: topaz-system
spec:
  replicas: 1
  selector:
    matchLabels:
      app: topaz
  template:
    metadata:
      labels:
        app: topaz
    spec:
      containers:
        - name: topaz
          image: thecloudtheory/topaz-host:latest
          args:
            - --certificate-file
            - /certs/topaz.crt
            - --certificate-key
            - /certs/topaz.key
          ports:
            - containerPort: 8899  # ARM
            - containerPort: 8898  # Key Vault
            - containerPort: 8891  # Blob Storage, Queue Storage, Table Storage
          volumeMounts:
            - name: certs
              mountPath: /certs
              readOnly: true
            - name: data
              mountPath: /app/.topaz
      volumes:
        - name: certs
          secret:
            secretName: topaz-tls
        - name: data
          persistentVolumeClaim:
            claimName: topaz-data
```

### Topaz service

A `ClusterIP` service makes Topaz reachable cluster-wide as `topaz.topaz-system.svc.cluster.local`. The service exposes all emulated ports.

```yaml title="topaz-service.yaml"
apiVersion: v1
kind: Service
metadata:
  name: topaz
  namespace: topaz-system
spec:
  selector:
    app: topaz
  ports:
    - name: arm
      port: 8899
      targetPort: 8899
    - name: keyvault
      port: 8898
      targetPort: 8898
    - name: storage
      port: 8891
      targetPort: 8891  # Blob, Queue, and Table Storage all share this port
```

### TLS secret

Create the secret from the Topaz certificate files before deploying:

```sh title="create-tls-secret.sh"
kubectl create secret generic topaz-tls \
  --from-file=topaz.crt=certificate/topaz.crt \
  --from-file=topaz.key=certificate/topaz.key \
  -n topaz-system \
  --dry-run=client -o yaml | kubectl apply -f -
```

The certificate files are available in the [`certificate/`](https://github.com/TheCloudTheory/Topaz/tree/main/certificate) directory of the Topaz repository.

## CoreDNS rewrite

Patch the `coredns` ConfigMap in `kube-system` to add two rewrite rules — one exact match for the ARM control-plane host and one regex rule for all data-plane SDK hostnames:

```text title="Corefile rules"
rewrite name exact topaz.local.dev topaz.topaz-system.svc.cluster.local
rewrite name regex (.+)\.topaz\.local\.dev topaz.topaz-system.svc.cluster.local answer auto
```

The full patch script is shown below. It is idempotent — safe to run multiple times.

```sh title="patch-coredns.sh"
MARKER="topaz.local.dev"
REWRITE_RULE="    rewrite name exact topaz.local.dev topaz.topaz-system.svc.cluster.local\n    rewrite name regex (.+)\\.topaz\\.local\\.dev topaz.topaz-system.svc.cluster.local answer auto"

CURRENT=$(kubectl get configmap coredns -n kube-system -o jsonpath='{.data.Corefile}')

if echo "$CURRENT" | grep -q "$MARKER"; then
  echo "Rewrite rule already present."
  exit 0
fi

PATCHED=$(echo "$CURRENT" | awk "/forward \\./{print \"$REWRITE_RULE\"}1")

kubectl patch configmap coredns -n kube-system \
  --type=merge \
  --patch "{\"data\":{\"Corefile\":$(printf '%s' "$PATCHED" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))')}}"

kubectl rollout restart deployment/coredns -n kube-system
```

:::info[Two rules are required]

The ARM client (`TopazArmClientOptions`) connects to `https://topaz.local.dev:8899`. This is the bare hostname — not a subdomain — so the `(.+)\.topaz\.local\.dev` regex does **not** match it. The `exact` rule covers this case.

:::

## Application code

Applications connect to Topaz exactly as they would in any other environment. The only Kubernetes-specific step is reading the subscription ID from an environment variable.

```csharp title="Startup.cs" showLineNumbers
var subscriptionId = Environment.GetEnvironmentVariable("TOPAZ_SUBSCRIPTION_ID")
    ?? throw new InvalidOperationException("TOPAZ_SUBSCRIPTION_ID is required");

var credential = new AzureLocalCredential(Globals.GlobalAdminId);

// Wait for Topaz to be ready, then register the subscription.
using var topazClient = new TopazArmClient(credential);
for (var attempt = 1; ; attempt++)
{
    if (await topazClient.CheckIfReadyAsync()) break;
    if (attempt >= 30) throw new TimeoutException("Topaz did not become ready.");
    await Task.Delay(TimeSpan.FromSeconds(2));
}
await topazClient.CreateSubscriptionAsync(Guid.Parse(subscriptionId), "my-app-sub");

// Use standard ARM + data-plane SDK clients — no endpoint overrides needed.
var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
```

Because CoreDNS rewrites the hostnames, `TopazArmClientOptions.New` and `TopazResourceHelpers.GetAzureStorageConnectionString(...)` work without modification.

### Trusting the Topaz certificate in Docker images

Application images must trust the Topaz certificate so that Azure SDK TLS connections succeed. Add the following to the app's `Dockerfile`:

```dockerfile title="Dockerfile"
COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates
```

Copy the certificate into the Docker build context before building:

```sh title="build image"
cp certificate/topaz.crt apps/my-service/topaz.crt
docker build -t my-service:latest apps/my-service/
```

## Complete example

A fully runnable example using k3d (k3s in Docker) with two sample services is available in the repository:

[`Examples/Topaz.Example.Kubernetes/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.Kubernetes)

The example includes:
- `setup.sh` — one-command cluster provisioning
- `k8s/` — all Kubernetes manifests and the CoreDNS patch script
- `apps/order-service/` — Blob Storage-backed orders API
- `apps/inventory-service/` — Queue Storage-backed inventory events API
