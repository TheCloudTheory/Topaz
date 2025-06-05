FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled
WORKDIR /app

COPY ./publish .

# Set environment variables
ENV ASPNETCORE_URLS="http://*:80;https://*:443"

CMD ["./topaz-linux-x64", "start", "--log-level", "Information"]