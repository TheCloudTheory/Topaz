# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore and publish in one step to avoid transitive package resolution issues
COPY app/ ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Trust the Topaz self-signed certificate so Azure SDK clients can connect
# to the emulated services without any code-level TLS overrides.
COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TopazComposeApp.dll"]
