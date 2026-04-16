FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files
COPY ["src/ConnectSphere.Post.API/ConnectSphere.Post.API.csproj", "src/ConnectSphere.Post.API/"]

# Restore dependencies
RUN dotnet restore "./src/ConnectSphere.Post.API/ConnectSphere.Post.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/ConnectSphere.Post.API"
RUN dotnet build "./ConnectSphere.Post.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConnectSphere.Post.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectSphere.Post.API.dll"]