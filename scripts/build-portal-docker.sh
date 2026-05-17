#!/bin/bash

# Default to arm64 if no platform is provided
PLATFORM=${1:-arm64}

# Validate platform parameter
if [[ "$PLATFORM" != "amd64" && "$PLATFORM" != "arm64" ]]; then
    echo "Error: Platform must be either 'amd64' or 'arm64'"
    echo "Usage: $0 [amd64|arm64]"
    exit 1
fi

echo "Building Portal for platform: $PLATFORM"

rm -rf ./publish

# Publish the Portal application (framework-dependent, platform-agnostic IL)
dotnet publish ./Topaz.Portal/Topaz.Portal.csproj -c Release -o ./publish

# Publish CLI for both architectures (self-contained single-file) so Dockerfile can select the right one
dotnet publish ./Topaz.CLI/Topaz.CLI.csproj -c Release -r linux-x64 -o ./publish
dotnet publish ./Topaz.CLI/Topaz.CLI.csproj -c Release -r linux-arm64 -o ./publish

docker build -f ./Topaz.Portal/Dockerfile -t topaz/portal --platform linux/$PLATFORM --no-cache --build-arg TARGETPLATFORM=linux/$PLATFORM .
