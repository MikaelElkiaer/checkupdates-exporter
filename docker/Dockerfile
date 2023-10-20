FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /src
COPY src/*.csproj .
RUN dotnet restore
COPY src .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as runtime
WORKDIR /app
COPY --from=build /app .
HEALTHCHECK --interval=5s --timeout=1s --start-period=5s --retries=3 CMD [ "curl", "-f", "http://localhost:8080/health" ]
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CheckupdatesExporter.dll"]
