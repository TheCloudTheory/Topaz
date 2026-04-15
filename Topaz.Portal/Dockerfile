FROM alpine AS base
WORKDIR /app

COPY ./publish .

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

EXPOSE 8080
EXPOSE 8081

COPY --from=base /app .

ENTRYPOINT ["dotnet", "Topaz.Portal.dll"]
