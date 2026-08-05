# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Trust the Topaz certificate so Azure SDK TLS connections succeed without code-level overrides.
COPY topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryService.dll"]
