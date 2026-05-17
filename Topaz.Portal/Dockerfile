FROM alpine AS base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then chmod +x topaz-linux-x64 && mv topaz-linux-x64 topaz && rm -f topaz-linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux/arm64" ]; then chmod +x topaz-linux-arm64 && mv topaz-linux-arm64 topaz && rm -f topaz-linux-x64; else echo "Not Arm64"; fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

EXPOSE 8080
EXPOSE 8081

COPY --from=base /app .

ENTRYPOINT ["dotnet", "Topaz.Portal.dll"]
