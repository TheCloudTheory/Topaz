FROM alpine as base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux-x64" ]; then chmod +x topaz-linux-x64 && mv topaz-linux-x64 topaz-cli && rm -f linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux-arm64" ]; then chmod +x topaz-linux-arm64 && mv topaz-linux-arm64 topaz-cli && rm -f linux-x64; else echo "Not Arm64"; fi

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled as final
WORKDIR /app

COPY --from=base /app .

# Set environment variables
ENV ASPNETCORE_URLS="http://*:80;https://*:443"

CMD ["./topaz-cli", "start", "--log-level", "Information"]