FROM alpine AS base
ARG TARGETPLATFORM
WORKDIR /app

COPY ./publish .

RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then chmod +x topaz-mcp-linux-x64 && mv topaz-mcp-linux-x64 topaz-mcp && rm -f topaz-mcp-linux-arm64; else echo "Not x64"; fi
RUN if [ "$TARGETPLATFORM" = "linux/arm64" ]; then chmod +x topaz-mcp-linux-arm64 && mv topaz-mcp-linux-arm64 topaz-mcp && rm -f topaz-mcp-linux-x64; else echo "Not Arm64"; fi
RUN mkdir -p .topaz && chmod -R 777 .topaz

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled AS final
WORKDIR /app

COPY --from=base /app .

# MCP server requires stdin to be open - run with: docker run -i topaz/mcp
LABEL description="Topaz MCP Server - Requires stdin open (use -i flag)"

ENTRYPOINT [ "./topaz-mcp" ]