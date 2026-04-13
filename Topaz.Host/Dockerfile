FROM alpine AS base
WORKDIR /app

COPY ./publish .

RUN chmod +x Topaz.Host && mv Topaz.Host topaz-host
RUN mkdir -p .topaz && chmod -R 777 .topaz

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

COPY --from=base /app .

ENTRYPOINT [ "./topaz-host" ]
CMD ["--log-level", "Information"]
