---
slug: devcontainer-topaz
title: "Building a devcontainer for Topaz: workspace mounts, DNS wildcards, and why /etc/resolv.conf always wins"
description: A technical account of building a VS Code Dev Container for the Topaz Azure emulator, covering Docker Compose workspace mount pitfalls, certificate distribution without bind mounts, and why dnsmasq belongs in its own sidecar service.
keywords: [devcontainer azure emulator, vscode devcontainer docker compose, dnsmasq docker sidecar, wildcard dns devcontainer, topaz local azure development, devcontainer dns resolution]
authors: kamilmrzyglod
tags: [general, cicd]
---

I wanted the "Open in Dev Container" badge for Topaz to do the obvious thing: open the repository in VS Code with the emulator already running, the certificates trusted, and `*.topaz.local.dev` resolving without any manual setup. That target experience sounds simple. Getting there was not.

The tricky part was not Docker Compose itself. The tricky part was figuring out why workspace mounts were unreliable in Compose mode, how to distribute certificates without depending on bind mounts, and why `/etc/resolv.conf` kept defeating otherwise reasonable DNS ideas. This post is a technical account of building the Topaz devcontainer, the three services that ended up in the Docker Compose file, and the architecture that finally worked.

{/* truncate */}

## What we were building

Topaz is a single binary that emulates Azure Storage, Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, RBAC, ARM, and Entra ID. The devcontainer goal was straightforward: when a developer opens the repository in VS Code, Topaz Host should already be running, all the service ports should be reachable, TLS certificates should be trusted, and DNS wildcards for `*.topaz.local.dev` should resolve, without any manual setup steps.

The target end state was a developer terminal like this:

```bash
vscode ➜ /workspaces/Topaz $ topaz health
{"workingDirectory":"/app","version":"...","status":"Healthy"}

vscode ➜ /workspaces/Topaz $ curl https://topaz.local.dev:8899/health
{"workingDirectory":"/app","version":"...","status":"Healthy"}

vscode ➜ /workspaces/Topaz $ curl https://my-vault.vault.topaz.local.dev:8898/secrets
{"value":[],"nextLink":null}
```

The second and third commands, which use hostnames rather than IP addresses, are the ones that turned out to be the interesting engineering problem.

## The initial architecture: three services in Docker Compose

The devcontainer uses Docker Compose mode, which VS Code's Dev Containers extension supports directly. The natural structure maps to three services:

```yaml
services:
  devcontainer:   # the VS Code workspace container
  topaz:          # the Topaz Host sidecar
  dns-sidecar:    # wildcard DNS resolver (more on this later)
```

All three share a bridge network with a fixed subnet (`172.28.0.0/16`) and static IP assignments:
- `devcontainer` at `172.28.0.2`
- `topaz` at `172.28.0.10`
- `dns-sidecar` at `172.28.0.53`

The fixed IPs are important. DNS configuration needs to point at an address that is stable before any container starts, and the `address=/.topaz.local.dev/172.28.0.10` dnsmasq rule needs to know where Topaz lives without dynamic lookup.

## Problem one: workspace mount in Compose mode

The first thing that breaks when you move a devcontainer to Docker Compose mode is workspace mounting. In a single-container devcontainer, VS Code automatically bind-mounts your local workspace folder using the `workspaceMount` property. In Compose mode, VS Code tries to inject a workspace mount into a generated override compose file, but the injection is unreliable, particularly when the workspace is on an external drive or a path the container runtime does not have in its file-sharing configuration.

The first sign of the problem is an error like:

```
OCI runtime exec failed: exec failed: unable to start container process:
chdir to cwd ("/workspaces/Topaz") set in config.json failed: no such file or directory
```

The `/workspaces/Topaz` directory does not exist because the bind mount never happened. Adding `workspaceMount` to `devcontainer.json` looks like it should help, but the Dev Containers schema explicitly does not allow it in Compose mode, so VS Code will flag it as an unknown property and ignore it.

The reliable fix is to declare the workspace mount explicitly in the Docker Compose file:

```yaml
devcontainer:
  image: mcr.microsoft.com/devcontainers/base:ubuntu
  volumes:
    - ../:/workspaces/Topaz:cached
  command: sleep infinity
```

The `..` is relative to `.devcontainer/`, so it resolves to the repository root. `workspaceFolder` in `devcontainer.json` must match the target path exactly:

```json
"workspaceFolder": "/workspaces/Topaz"
```

This works when the container runtime can access the source path. When it cannot, because you are using Colima and an external drive is not in its mount list, Docker creates the bind-mount target as an empty directory rather than failing loudly. That is how we ended up with `topaz.crt` and `topaz.key` appearing as empty directories inside the container instead of files. Solving that for certificates is what led to the second problem.

## Problem two: distributing TLS certificates without bind mounts

Topaz requires a TLS certificate to start. The certificate files live in `certificate/topaz.crt` and `certificate/topaz.key` in the repository. The straightforward approach, bind-mounting them into the Topaz sidecar, fails on Colima (and on Docker Desktop when the path is outside the configured file-sharing list) for the same reason workspace mounts fail: the bind mount silently becomes an empty directory.

The [Docker Compose example in the repository](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Compose) solved this a different way: using `docker cp` to populate a named volume, the same mechanism Testcontainers' `WithResourceMapping` uses internally. A named volume is always accessible to containers regardless of which paths the runtime has permission to bind-mount from the host.

The devcontainer uses the same pattern. A shell script at `.devcontainer/init-certs.sh` runs via `initializeCommand` in `devcontainer.json`:

```json
"initializeCommand": "bash .devcontainer/init-certs.sh"
```

`initializeCommand` runs on the **host machine** before any container starts, which means it has access to the certificate files at their actual paths:

```bash
VOLUME="topaz-devcontainer-certs"
CONTAINER=$(docker create -v "$VOLUME:/certs" alpine)
docker cp "$SCRIPT_DIR/topaz.crt" "$CONTAINER:/certs/topaz.crt"
docker cp "$SCRIPT_DIR/topaz.key" "$CONTAINER:/certs/topaz.key"
docker rm "$CONTAINER"
```

The Topaz sidecar then mounts the named volume rather than bind-mounting the files directly:

```yaml
topaz:
  volumes:
    - topaz-devcontainer-certs:/certs:ro
    - topaz-data:/app/.topaz
  command:
    - --certificate-file
    - /certs/topaz.crt
    - --certificate-key
    - /certs/topaz.key
```

The named volume is declared as `external: true` in the compose file so Docker Compose does not try to create it (the `initializeCommand` already did that).

## Problem three: DNS for *.topaz.local.dev

This was the longest part of the investigation. Topaz services are reached at subdomains of `topaz.local.dev`, such as `my-vault.vault.topaz.local.dev`, `myaccount.blob.storage.topaz.local.dev`, and `myregistry.cr.topaz.local.dev`, and these need to resolve to the Topaz sidecar IP inside the devcontainer. There is no wildcard support in `/etc/hosts`, so a single `extra_hosts` entry for `topaz.local.dev` is not enough; every named resource would need a manual entry.

The approach we tried first, failed, tried second, and failed in a different way before arriving at the working solution is worth describing in order.

### Attempt 1: extra_hosts in docker-compose.yml

Docker Compose supports `extra_hosts`, which injects entries into `/etc/hosts`. The initial configuration added `topaz.local.dev` to the devcontainer service:

```yaml
devcontainer:
  extra_hosts:
    - "topaz.local.dev:172.28.0.10"
```

This did not work, not because the mechanism is wrong, but because VS Code generates a compose override file when it starts the devcontainer, and the override appears to discard or override `extra_hosts` from the base file. The host entry never appeared in `/etc/hosts` inside the container.

Even if it had worked, it would only have solved `topaz.local.dev`. Every vault, every storage account, every registry would still need a manual entry. The `extra_hosts` approach was the wrong layer entirely.

### Attempt 2: dnsmasq installed inside the devcontainer

The next approach was to install `dnsmasq` inside the devcontainer via `postCreateCommand` and configure it to resolve `*.topaz.local.dev` to `172.28.0.10`. This is how the existing `install-linux.sh` script works for non-container Linux installs.

The first failure was a port 53 conflict. The devcontainer base image runs `systemd-resolved` or has a stub resolver already listening on UDP port 53. Killing it with `systemctl stop systemd-resolved` produced:

```
"systemd" is not running in this container due to its overhead.
Use the "service" command to start services instead.
```

systemd does not run in devcontainers. The next attempt used `fuser -k 53/udp` to kill whatever was on the port, which worked as a one-time fix.

The second failure was `/etc/resolv.conf`. To make the container query our dnsmasq instance first, we needed to prepend `nameserver 127.0.0.1` to `/etc/resolv.conf`. The file appeared writable, and our `tee` overwrote it, but when a new shell opened, the change was gone. Docker bind-mounts `/etc/resolv.conf` from a file managed by the container runtime. You can overwrite it with `tee` within a single process's lifetime, but the next container process re-reads it from the bind-mounted source. You cannot delete it (`rm` fails with "Device or resource busy"). Any content written to it via `tee` in a `postCreateCommand` is effectively transient.

The third failure was timing. `postCreateCommand` runs once after container creation. `postStartCommand` runs on every start. dnsmasq installed during `postCreateCommand` would not be running when the container was restarted, and `postStartCommand` cannot install dnsmasq (apt is not available until `postCreateCommand` has run). The lifecycle ordering makes in-container dnsmasq unreliable after a restart regardless of the resolv.conf problem.

### The working solution: dnsmasq as a sidecar service

The correct layer for this problem is not inside the devcontainer. It is in Docker Compose. Docker sets `/etc/resolv.conf` based on the compose `dns:` directive **before** any container process starts, and the value persists for the lifetime of the container without anything inside the container being able to overwrite it.

The solution is a dedicated DNS sidecar, a minimal alpine container running dnsmasq, and a `dns:` entry on the devcontainer service pointing at it:

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

devcontainer:
  dns:
    - 172.28.0.53    # topaz DNS sidecar (resolves *.topaz.local.dev)
    - 1.1.1.1        # fallback internet DNS
  depends_on:
    - dns-sidecar
```

When Docker Compose starts the devcontainer service, it writes:

```
nameserver 172.28.0.53
nameserver 1.1.1.1
```

into the container's `/etc/resolv.conf`. This is a file Docker controls, not one we are trying to modify from inside the container. Any DNS lookup that reaches `172.28.0.53` gets the `address=/.topaz.local.dev/172.28.0.10` answer for Topaz subdomains and forwards everything else to `1.1.1.1`.

The `depends_on` ensures the DNS sidecar is up before the devcontainer tries to use it. The `restart: unless-stopped` keeps it running across container restarts. Because it is a separate service, the dnsmasq lifecycle is entirely independent of anything happening in `postCreateCommand` or `postStartCommand`.

After this change:

```bash
vscode ➜ /workspaces/Topaz $ curl https://topaz.local.dev:8899/health
{"workingDirectory":"/app","version":"...","status":"Healthy"}

vscode ➜ /workspaces/Topaz $ curl https://my-vault.vault.topaz.local.dev:8898/secrets
{"value":[],"nextLink":null}
```

No hosts-file entries. No manual entries per resource. Any resource created in Topaz with any name resolves automatically.

## What postCreate.sh does now

With DNS and certificate distribution handled at the compose layer, `postCreateCommand` is left with a smaller, cleaner set of responsibilities:

1. **Trust the Topaz TLS certificate** in the Ubuntu system CA store. This is a one-time operation. `update-ca-certificates` ingests the cert so that `curl`, the Azure SDK, and any other TLS client that uses the system store trusts Topaz's self-signed certificate without `--insecure`.

2. **Inject the certificate into the Azure CLI certifi bundle.** The Azure CLI ships its own bundled `cacert.pem` separate from the system store. Without this step, `az rest --url https://topaz.local.dev:8899/...` fails with an SSL error even though `curl` works fine. The script finds the bundle at the path the `azure-cli` devcontainer feature installs it (`/opt/az/lib/python*/site-packages/certifi/cacert.pem`) and appends the Topaz cert.

3. **Install the Topaz CLI.** The CLI (`topaz`) and host binary (`topaz-host`) are downloaded from the GitHub release. The version is resolved from `/releases` rather than `/releases/latest` because Topaz is currently in beta and beta releases do not appear at the `latest` endpoint. The install is best-effort. If it fails (wrong architecture, network issue), the script continues rather than aborting the entire `postCreateCommand`.

4. **Write shell environment variables.** `AZURE_TENANT_ID` (the Topaz default tenant) and `REQUESTS_CA_BUNDLE` (pointing at the system certificate store for Python-based tools) are appended to `~/.bashrc` idempotently.

## The final compose structure

```yaml
networks:
  topaz-net:
    driver: bridge
    ipam:
      config:
        - subnet: "172.28.0.0/16"

volumes:
  topaz-data: {}
  topaz-devcontainer-certs:
    external: true

services:
  devcontainer:
    image: mcr.microsoft.com/devcontainers/base:ubuntu
    volumes:
      - ../:/workspaces/Topaz:cached
    command: sleep infinity
    dns:
      - 172.28.0.53
      - 1.1.1.1
    depends_on:
      - dns-sidecar
    networks:
      topaz-net:
        ipv4_address: "172.28.0.2"

  topaz:
    image: thecloudtheory/topaz-host:latest
    platform: linux/amd64
    networks:
      topaz-net:
        ipv4_address: "172.28.0.10"
    ports:
      - "8899:8899"
      - "8898:8898"
      - "8892:8892"
      - "8891:8891"
      # ... all service ports
    volumes:
      - topaz-devcontainer-certs:/certs:ro
      - topaz-data:/app/.topaz
    command:
      - --certificate-file
      - /certs/topaz.crt
      - --certificate-key
      - /certs/topaz.key

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

Three services. Two volumes. One network. The certificate distribution happens before any container starts (`initializeCommand`). The DNS configuration happens when Docker Compose starts the devcontainer service (`dns:`). The certificate trust and CLI install happen once after container creation (`postCreateCommand`).

## The constraints that shaped the design

Most of the intermediate failures came from the same underlying constraint: **inside a running container, the parts of `/etc/resolv.conf` and `/etc/hosts` that Docker manages are not ours to own.** Docker bind-mounts both files. You can read them, you can overwrite them with a new process, but the bind mount source is what Docker wrote when it started the container, and that is what survives restarts, new shells, and exec sessions.

The only way to control what nameservers a Docker container queries is to set `dns:` in the compose service definition before the container starts. Once it is running, you are working around a constraint rather than owning the solution. Every in-container approach, dnsmasq in `postCreateCommand`, `tee` to `/etc/resolv.conf`, `fuser` to free port 53, is a workaround with a lifecycle problem attached to it. The sidecar approach is not.

The same logic applies to the workspace mount. In Compose mode, the workspace bind mount is injected by VS Code into a generated override file. The injection is sometimes unreliable, particularly on external drives or when the container runtime has not been configured to share those paths. Declaring the mount explicitly in the base compose file makes it deterministic. The explicit declaration wins over whatever VS Code's override would have injected.

## What this enables

The devcontainer is the easiest path to a Topaz environment for anyone who does not want to go through the certificate trust and DNS configuration steps manually. Click the badge, wait for the build, open a terminal:

```bash
# DNS wildcard resolves automatically, no hosts file editing
vscode ➜ /workspaces/Topaz $ topaz health
{"status":"Healthy"}

# HTTPS with the Topaz cert trusted, no --insecure needed
vscode ➜ /workspaces/Topaz $ az rest --method get \
  --url "https://topaz.local.dev:8899/subscriptions?api-version=2020-01-01"

# Named resources work immediately after creation
vscode ➜ /workspaces/Topaz $ az keyvault create \
  --name my-vault --resource-group rg-dev --location westeurope
vscode ➜ /workspaces/Topaz $ curl https://my-vault.vault.topaz.local.dev:8898/secrets?api-version=7.4
```

The devcontainer is available at the root of the repository and as a standalone copy-into-your-project template in [`Examples/Devcontainer/`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Devcontainer). The standalone template bundles its own certificate copy so it has no dependency on the `certificate/` directory. Teams can drop `.devcontainer/` into any project, add their resource names as needed, and get the same environment.

:::tip[Try the devcontainer]
Click the badge below from the Topaz repository page to open a fully configured local Azure environment in VS Code, with Topaz Host running, DNS wired, certificates trusted, and Azure CLI ready.

[![Open in Dev Containers](https://img.shields.io/static/v1?label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/TheCloudTheory/Topaz)

Not set up for devcontainers yet? [Getting started with Dev Containers →](https://code.visualstudio.com/docs/devcontainers/containers) · [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::
