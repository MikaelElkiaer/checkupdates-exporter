FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /src
COPY src/*.csproj .
RUN dotnet restore
COPY src .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0 as runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CheckupdatesPrometheusExporter.dll"]
