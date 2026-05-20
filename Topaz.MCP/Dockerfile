FROM alpine AS base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then chmod +x topaz-mcp-linux-x64 && mv topaz-mcp-linux-x64 topaz-mcp && rm -f topaz-mcp-linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux/arm64" ]; then chmod +x topaz-mcp-linux-arm64 && mv topaz-mcp-linux-arm64 topaz-mcp && rm -f topaz-mcp-linux-x64; else echo "Not Arm64"; fi
RUN mkdir -p .topaz && chmod -R 777 .topaz

# Install the Topaz self-signed certificate into an Ubuntu stage so that
# update-ca-certificates can be run (not available in the chiseled final image).
FROM ubuntu:noble AS cert-builder
RUN apt-get update -qq && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
COPY --from=base /app/topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
WORKDIR /app

# Trust the Topaz self-signed certificate so Azure SDK clients resolve the emulator
# endpoints without any code-level TLS bypass.
COPY --from=cert-builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/ca-certificates.crt
COPY --from=base /app .

# MCP server requires stdin to be open.
# When the Topaz host is also running as a container (started via RunTopazAsContainer),
# join the shared topaz-net network AND point to the Topaz DNS resolver so that all
# *.topaz.local.dev subdomains (Key Vault, Storage, Service Bus, Event Hub) resolve correctly:
#   docker run -i --network topaz-net --dns 172.28.0.53 topaz/mcp
# If Topaz is running directly on the host (not in Docker), no --network or --dns flag is needed.
LABEL description="Topaz MCP Server - Requires stdin open (use -i flag)"

ENTRYPOINT [ "./topaz-mcp" ]