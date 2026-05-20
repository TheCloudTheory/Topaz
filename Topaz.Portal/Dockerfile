FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

ENV TOPAZ_CONTAINERIZED=true

EXPOSE 8080
EXPOSE 8081

COPY ./publish .

ENTRYPOINT ["dotnet", "Topaz.Portal.dll"]
