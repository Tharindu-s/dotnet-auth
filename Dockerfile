# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy solution and project files
COPY dotnet-auth.sln ./
COPY auth.webapi/*.csproj ./auth.webapi/

# Restore dependencies for Web API project
RUN dotnet restore auth.webapi/auth.webapi.csproj

# Copy all source files for Web API project
COPY auth.webapi/. ./auth.webapi/

# Build and publish Web API project (Release)
RUN dotnet publish auth.webapi/auth.webapi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

# Writable folder for a generated signing key (JWT__PrivateKeyPath=/app/data/signing-key.pem).
# docker-compose mounts a volume here; a new named volume inherits this ownership.
RUN mkdir -p /app/data && chown $APP_UID /app/data

# Run as the image's non-root user
USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "auth.webapi.dll"]

