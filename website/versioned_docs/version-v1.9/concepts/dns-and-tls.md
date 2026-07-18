---
sidebar_position: 3
description: Why Topaz requires DNS configuration and a trusted certificate — the mechanics behind hostname-based Azure emulation and what the TLS wildcard certificate covers.
keywords: [topaz dns setup, topaz certificate, azure emulator tls, topaz tls certificate, local azure dns, azure wildcard certificate local]
---

# DNS and TLS in Topaz

Topaz's setup involves two one-time steps that might seem unusual for local software: configuring DNS and trusting a certificate. Both are a direct consequence of how Azure client libraries address services — and how Topaz intercepts that traffic without changing the client code.

## Why Azure clients use hostnames

Azure services are addressed by hostname, not by IP address. This is not incidental — it is load-balanced, tenant-scoped routing that makes multi-tenancy and service isolation work at Azure's scale.

When your code creates a `SecretClient` for `myvault.vault.azure.net`, the SDK resolves that hostname via DNS, connects to the resulting IP over HTTPS, presents the hostname in the TLS SNI field, and sends an authenticated request. The IP could be one of thousands of servers in Azure's fleet; what identifies the vault is the hostname, not the address.

## How Topaz intercepts the traffic

Topaz replaces the DNS resolution step. Instead of `myvault.vault.azure.net` resolving to an Azure IP, it resolves to `127.0.0.1`. Topaz listens on that address and handles the request.

To avoid conflicts with real Azure (you may still want to call `management.azure.com` for other purposes), Topaz uses a separate domain family: `*.topaz.local.dev`. A vault is accessed at `myvault.vault.topaz.local.dev:8898` rather than `myvault.vault.azure.net`. The DNS setup script adds entries for all of these Topaz-specific domains.

The one-time DNS script writes resolver rules so that `*.topaz.local.dev` always resolves to `127.0.0.1`. Once written, this requires no further changes per project or per service.

## The wildcard certificate

HTTPS requires a valid TLS certificate that matches the hostname the client is connecting to. Topaz bundles a self-signed wildcard certificate that covers the `*.topaz.local.dev` domain tree.

Because it is self-signed, client libraries — the Azure SDK, the Azure CLI, Terraform providers — will reject connections unless the certificate is added to the system trust store. That is what the "trust the certificate" setup step does: it installs `topaz.crt` into the OS trust store so that all tools on the machine recognise Topaz's certificate as valid.

This is a one-time operation per machine (or per WSL instance). Once trusted, any tool that uses the OS trust store — including `az`, `terraform`, and .NET apps using `HttpClient` — will connect to Topaz without certificate errors.

## Docker and the HTTP alternative

When running Topaz as a Docker container, the certificate is handled automatically inside the container. The container runtime terminates TLS internally and can be configured to expose HTTP-only ports to the host. In that setup the certificate trust step can be skipped for data-plane services that use HTTP ports.

The ARM control plane (port 8899) still uses HTTPS and requires the certificate to be trusted if called from tooling running on the host (e.g. Terraform running on the host machine targeting a Topaz container).

## The ROPC proxy

`az login --username --password` (Resource Owner Password Credentials) sends a login request to a Microsoft endpoint. Because this is a real outbound HTTPS connection that Topaz cannot intercept purely via DNS, `topaz-host` starts a lightweight HTTP CONNECT proxy on port 44380 at the same time.

Setting `HTTPS_PROXY=http://127.0.0.1:44380` before running `az login` routes that connection through Topaz's proxy, which handles the Entra authentication locally and returns a valid token. All other `CONNECT` requests are passed through to the real internet unchanged.

## Bring-your-own certificate

In environments where the bundled certificate cannot be trusted (corporate PKI, managed device policies), you can supply your own PEM-encoded certificate and private key via the `--certificate-file` and `--certificate-key` flags on `topaz-host`. The certificate must cover the `*.topaz.local.dev` domain, or whichever domain family you have configured DNS for.

See [Getting started](../intro.md) for the exact trust commands per platform.
