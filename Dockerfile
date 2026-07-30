# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NaPoso/NaPoso.sln .
COPY NaPoso/NaPoso/NaPoso.csproj NaPoso/NaPoso/
RUN dotnet restore NaPoso/NaPoso/NaPoso.csproj

COPY NaPoso/ NaPoso/
WORKDIR /src/NaPoso/NaPoso
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NaPoso.dll"]
