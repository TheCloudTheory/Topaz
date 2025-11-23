FROM alpine AS base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then chmod +x topaz-linux-x64 && mv topaz-linux-x64 topaz-cli && rm -f topaz-linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux/arm64" ]; then chmod +x topaz-linux-arm64 && mv topaz-linux-arm64 topaz-cli && rm -f topaz-linux-x64; else echo "Not Arm64"; fi
RUN mkdir -p .topaz && chmod -R 777 .topaz

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

COPY --from=base /app .

ENTRYPOINT [ "./topaz-cli" ]
CMD ["start", "--log-level", "Information"]