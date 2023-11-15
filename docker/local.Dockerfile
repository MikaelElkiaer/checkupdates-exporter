FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
WORKDIR /app
COPY app .
ENV ASPNETCORE_URLS=http://+:8080
ADD --checksum=sha256:6ff339d8e5cd140cf96d229b0145e1ce946d592edae1ff21fc59df1fe56d9bf5 https://github.com/mikaelelkiaer/dotnet-healthchecker/releases/download/1.0.1/healthchecker.dll .
ADD --checksum=sha256:2264b15f4313dcedc50d325463216559f82ae0c894f8d0eab98f484223aaee46 https://github.com/mikaelelkiaer/dotnet-healthchecker/releases/download/1.0.1/healthchecker.deps.json .
ADD --checksum=sha256:aeb46a15366e38511c192ccf65312442c64e8753b3c16fd2ca0b4cb34a5c59f3 https://github.com/MikaelElkiaer/dotnet-healthchecker/releases/download/1.0.1/healthchecker.runtimeconfig.json .
HEALTHCHECK CMD [ "dotnet", "healthchecker.dll", "--", "http://localhost:8080/healthz" ]
ENTRYPOINT ["dotnet", "CheckupdatesExporter.dll"]
