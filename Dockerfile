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

EXPOSE 80

ENTRYPOINT ["dotnet", "auth.webapi.dll"]

