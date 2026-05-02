FROM alpine AS base
WORKDIR /app

COPY ./publish .

RUN chmod +x topaz-host-linux-x64 2>/dev/null || chmod +x topaz-host-linux-arm64 2>/dev/null; \
    mv topaz-host-linux-x64 topaz-host 2>/dev/null || mv topaz-host-linux-arm64 topaz-host 2>/dev/null
RUN mkdir -p .topaz && chmod -R 777 .topaz

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

COPY --from=base /app .

ENTRYPOINT [ "./topaz-host" ]
CMD ["--log-level", "Information"]
