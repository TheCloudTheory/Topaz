#!/bin/bash

# Default to arm64 if no platform is provided
PLATFORM=${1:-arm64}

# Validate platform parameter
if [[ "$PLATFORM" != "amd64" && "$PLATFORM" != "arm64" ]]; then
    echo "Error: Platform must be either 'amd64' or 'arm64'"
    echo "Usage: $0 [amd64|arm64]"
    exit 1
fi

# Map platform names for dotnet runtime identifiers
if [[ "$PLATFORM" == "amd64" ]]; then
    DOTNET_RID="linux-x64"
else
    DOTNET_RID="linux-arm64"
fi

echo "Building for platform: $PLATFORM (dotnet RID: $DOTNET_RID)"

rm -rf ./publish
dotnet publish ./Topaz.MCP/Topaz.MCP.csproj -c Release -r $DOTNET_RID -o ./publish
docker build -f ./Topaz.MCP/Dockerfile -t topaz/mcp --platform linux/$PLATFORM --no-cache --build-arg TARGETPLATFORM=linux/$PLATFORM .