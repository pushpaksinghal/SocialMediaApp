FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files
COPY ["src/ConnectSphere.Like.API/ConnectSphere.Like.API.csproj", "src/ConnectSphere.Like.API/"]

# Restore dependencies
RUN dotnet restore "./src/ConnectSphere.Like.API/ConnectSphere.Like.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/ConnectSphere.Like.API"
RUN dotnet build "./ConnectSphere.Like.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConnectSphere.Like.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectSphere.Like.API.dll"]