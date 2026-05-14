FROM alpine AS base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then chmod +x topaz-host-linux-x64 && mv topaz-host-linux-x64 topaz-host && rm -f topaz-host-linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux/arm64" ]; then chmod +x topaz-host-linux-arm64 && mv topaz-host-linux-arm64 topaz-host && rm -f topaz-host-linux-x64; else echo "Not Arm64"; fi
RUN mkdir -p .topaz && chmod -R 777 .topaz

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

COPY --from=base /app .

ENTRYPOINT [ "./topaz-host" ]
CMD ["--log-level", "Information"]
