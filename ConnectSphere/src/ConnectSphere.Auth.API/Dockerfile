# Stage 1: Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first (for better caching)
COPY ["src/ConnectSphere.Auth.API/ConnectSphere.Auth.API.csproj", "src/ConnectSphere.Auth.API/"]

# Restore dependencies
RUN dotnet restore "./src/ConnectSphere.Auth.API/ConnectSphere.Auth.API.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/ConnectSphere.Auth.API"
RUN dotnet build "./ConnectSphere.Auth.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConnectSphere.Auth.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set entry point
ENTRYPOINT ["dotnet", "ConnectSphere.Auth.API.dll"]