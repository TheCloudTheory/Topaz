#!/bin/bash

rm -rf ./publish
dotnet publish ./Topaz.CLI/Topaz.CLI.csproj -c Release -r linux-arm64 -o ./publish
docker build -f ./Topaz.CLI/Dockerfile -t topaz/cli --platform linux/arm64 --progress=plain --no-cache --build-arg TARGETPLATFORM=linux/arm64 .
