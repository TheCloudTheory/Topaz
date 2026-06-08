---
slug: ropc-local-azure-login
title: "How Topaz enables az login without root: MSAL, port 443, and a built-in CONNECT proxy"
description: Topaz cannot bind port 443 without root on non-Docker installs. MSAL's user-realm discovery pre-flight always targets port 443, which breaks ROPC login. This post explains the constraint, why the naive solutions do not work, and how a built-in HTTP CONNECT proxy solves it.
keywords: [az login local, azure local authentication, msal ropc local, az login username password local, topaz authentication, azure emulator login, local azure development authentication]
authors: kamilmrzyglod
tags: [general, entra]
---

One of the fundamental rules of Topaz is that it must not require `sudo` or admin rights. There is nothing more frustrating than having to request elevated permissions on your machine just to run a dev tool.

For most Azure CLI operations this is not a problem. You point the CLI at `https://topaz.local.dev:8899`, it talks to port 8899, done. ROPC login is the exception - `az login --username --password` triggers a user-realm discovery pre-flight inside MSAL that always targets port 443, regardless of what port you configured in the authority URL. On a non-Docker Topaz install, nothing is listening on port 443, because binding that port requires root. The result is a connection timeout that surfaces as an opaque account-not-found error with no indication that port 443 is involved at all.

This post explains the MSAL assumption behind that behavior, why the straightforward fixes do not work without elevated permissions, and how a built-in HTTP CONNECT proxy on port 44380 closes the gap cleanly.

{/* truncate */}

:::tip[Coming in v1.6]
ROPC authentication via the built-in CONNECT proxy is coming in Topaz v1.6. For non-container installs, you will set `HTTPS_PROXY=http://127.0.0.1:44380` before running:

```bash
az login --username topazadmin@topaz.local.dev --password admin
```

Docker installs do not need the proxy: port 443 is bound directly.

```bash
brew tap thecloudtheory/topaz && brew install topaz   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

[Entra ID emulation docs →](https://topaz.thecloudtheory.com/docs/intro/) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## Background: why ROPC matters locally

Topaz emulates Microsoft Entra ID as a first-class capability. That includes the token endpoint, the OIDC discovery document, and a built-in superadmin user (`topazadmin@topaz.local.dev`). The usual local authentication path is to configure the Azure CLI environment with a custom authority URL pointing at `https://topaz.local.dev:8899`, then call `az login --username --password` and let MSAL handle the rest. You may still use other flows or just leverage service principal credentials, but for many common scenarios, this is the easiest path.

This works correctly in Docker, where the container binds port 443 directly. On a non-container install (Homebrew on macOS, the `get-topaz.sh` script on Linux), port 443 requires root. Topaz runs as a regular user on port 8899. For everything except `az login`, that distinction does not matter: you tell the CLI the authority is `https://topaz.local.dev:8899`, the CLI talks to port 8899, done. For `az login`, it matters quite a lot, and the failure message does not tell you why (unless you're quite proficient with reading Python errors).

## The problem in practice

Take a standard non-Docker install with the Topaz host running on port 8899. The cloud environment is configured as:

```bash
az cloud register \
  --name Topaz \
  --endpoint-resource-manager "https://topaz.local.dev:8899" \
  --endpoint-active-directory "https://topaz.local.dev:8899/" \
  --endpoint-active-directory-resource-id "https://topaz.local.dev:8899"

az cloud set --name Topaz
az login --username topazadmin@topaz.local.dev --password admin
```

The error:

```
HTTPSConnectionPool(host='topaz.local.dev', port=443): Max retries exceeded with url:
/common/userrealm/topazadmin@topaz.local.dev?api-version=1.0
(Caused by NewConnectionError("HTTPSConnection(host='topaz.local.dev', port=443):
Failed to establish a new connection: [Errno 111] Connection refused"))
```

Port 443 is not listening, so the connection is immediately refused. MSAL was not talking to port 8899 at all.

My first assumption was a DNS issue. `topaz.local.dev` resolves to `127.0.0.1` via Topaz's `dnsmasq` configuration, which is straightforward. I confirmed the host entry was in place and that a direct `curl https://topaz.local.dev:8899/.well-known/openid-configuration` returned the expected OIDC document.

It was not DNS.

## What MSAL does before requesting a token

Before MSAL attempts the actual ROPC token exchange (`grant_type=password` to the `/token` endpoint), it makes a preliminary call. The call is called the user-realm discovery pre-flight:

```
GET https://topaz.local.dev/common/userrealm/topazadmin%40topaz.local.dev?api-version=1.0
```

The purpose is to determine whether the account uses federated authentication or a managed (password) flow. If the pre-flight fails, MSAL never proceeds to the token request. It returns an error that looks like an account-not-found or a connectivity problem, depending on whether the timeout fires first.

The key detail is in the URL. Notice the hostname: `topaz.local.dev`, with no port. MSAL builds this URL from the authority it was given, but it strips the port number. The reasoning, as far as I can tell from reading the MSAL source, is that user-realm discovery is supposed to hit `login.microsoftonline.com` at the default HTTPS port. For real Azure, this is reasonable: the endpoint is always on port 443. For a local emulator on port 8899, the assumption is simply wrong.

The pre-flight goes to port 443. Nothing is listening there, so the connection is refused immediately. MSAL surfaces this as a `ConnectionError` wrapping urllib3's `NewConnectionError`, which is not the most intuitive message when the authority URL you configured looks perfectly correct.

## Verifying the behavior

The stacktrace makes the cause explicit. MSAL's `authority.py` constructs the user-realm URL using `self.instance`, which is the hostname with the port stripped:

```python
"https://{netloc}/common/userrealm/{username}?api-version=1.0".format(
    netloc=self.instance, username=username)
```

`self.instance` is `topaz.local.dev`, not `topaz.local.dev:8899`. The request goes to port 443, port 443 is not bound, and urllib3 gets an immediate `[Errno 111] Connection refused`. There is no configuration option in MSAL to override this. The only way to make the request reach Topaz is to make port 443 answer and forward it to port 8899.

## Why simply binding port 443 does not work on non-root installs

The Docker path is easy: the container starts as root, binds port 443, and everything just works. On a regular user install, you cannot bind ports below 1024 without elevated permissions on Linux (and macOS, for that matter). Requiring `sudo` to run a dev tool is a non-starter: it means the tool can never be part of an automated workflow and creates a new set of questions about what exactly it does with root access.

The first alternative I considered was a `CAP_NET_BIND_SERVICE` capability flag on the binary. That would let the process bind port 443 without being root. It works, but it means every install method has to set the capability flag, and uninstalling cleanly becomes complicated. It also means that installation needs to happen with elevated permissions. Acceptable to some extent but still far from my original rule of avoiding `sudo` whenever possible.

The approach that worked without either root or capability flags is an HTTP CONNECT proxy.

## The fix: an HTTP CONNECT proxy on port 44380

When an HTTP client uses a proxy for HTTPS, it does not send the raw request to the proxy. Instead, it sends a `CONNECT` request:

```
CONNECT topaz.local.dev:443 HTTP/1.1
Host: topaz.local.dev:443
```

The proxy acknowledges with `200 Connection established` and then tunnels the TCP stream between the client and the real destination. In Topaz's case, the "real destination" is not actually on port 443. It is Kestrel on port 8899. The proxy intercepts the tunnel request and redirects it there.

Starting with v1.6, Topaz will start a lightweight TCP listener on `127.0.0.1:44380` alongside the main host. It reads the `CONNECT` request and applies three routing rules in order: any request targeting `topaz.local.dev:443` or a subdomain of it at port 443 is remapped to `127.0.0.1:8899`; any request targeting a Topaz subdomain at any other port is tunneled directly to loopback at that port; everything else is passed through transparently to the resolved destination. No certificate is involved at this stage, since the proxy operates below TLS. The actual TLS handshake happens between MSAL and Kestrel directly, after the tunnel is established.

From MSAL's perspective, it sent a `CONNECT` to the proxy, got a `200`, performed a TLS handshake with the server on the other end (which is Kestrel presenting the Topaz certificate), and received a response to the user-realm discovery request. It has no visibility into the fact that the TCP tunnel was redirected.

To activate the proxy:

```bash
export HTTPS_PROXY=http://127.0.0.1:44380
az login --username topazadmin@topaz.local.dev --password admin
```

That is the entire change. No elevated permissions. No modified hosts file entry. The proxy is already running alongside the Topaz host process, so you just need to tell the Azure CLI to use it.

## What the successful flow looks like

With the proxy in place, the sequence is:

1. MSAL sends `CONNECT topaz.local.dev:443` to `127.0.0.1:44380`.
2. The proxy opens a TCP connection to `127.0.0.1:8899` and replies `200 Connection established`.
3. MSAL performs a TLS handshake over the tunnel. Kestrel presents the Topaz certificate.
4. MSAL sends `GET /common/userrealm/topazadmin%40topaz.local.dev?api-version=1.0`. Topaz returns a managed realm response.
5. MSAL sends `POST /organizations/oauth2/v2.0/token` with `grant_type=password` and the credentials. Topaz validates and issues a token.
6. `az login` completes and the token is cached.

The Azure CLI then behaves identically to a Docker-based setup. ARM calls go to port 8899, Key Vault calls go to port 8898, everything that already worked continues to work.

## One thing that still does not work

The `HTTPS_PROXY` environment variable affects all HTTPS traffic for the current shell session, not just Azure CLI calls. If you have other services or tools running in the same session that make HTTPS requests, they will route through the Topaz proxy as well. The proxy passes through everything that is not targeting a Topaz hostname, so in practice this is harmless: other requests get tunneled normally and you do not notice the difference. But it is worth being aware of if you are troubleshooting network issues in a mixed session.

For CI pipelines, scoping the environment variable to the `az login` step specifically is cleaner:

```bash
HTTPS_PROXY=http://127.0.0.1:44380 
az login \
  --username topazadmin@topaz.local.dev \
  --password admin
```

After the CLI caches the token, unset the variable. Subsequent `az` commands do not trigger the user-realm pre-flight again, since they use the cached token until it expires.

## Why this problem did not show up earlier

Docker-based Topaz usage has always worked because the container binds port 443 directly. Most of the automated test infrastructure runs against the Docker image, including the Azure CLI integration tests and the PowerShell tests. The non-container install path is newer, used more heavily in local developer setups where someone is running `brew install topaz` or following the installation script, and the ROPC flow is only needed when you want `az login --username --password` rather than other authentication methods.

As it turns out, the two install paths have subtly different operational characteristics, and this was one of the gaps.

The test coverage for the non-container path was simply missing. Every existing Azure CLI test fixture starts Topaz with `TOPAZ_CONTAINERIZED=true`, which causes the container to bind port 443 directly. That meant ROPC login always worked in tests, and nothing caught that it would not work for anyone running Topaz locally outside of Docker. A separate fixture that starts Topaz without the containerized flag was needed to exercise the actual non-container behavior. Without that, the proxy is the right fix, but you have no way to know the problem exists until someone actually tries it.
