FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ConnectSphere/src/ConnectSphere.Feed.API/ConnectSphere.Feed.API.csproj", "ConnectSphere/src/ConnectSphere.Feed.API/"]
RUN dotnet restore "ConnectSphere/src/ConnectSphere.Feed.API/ConnectSphere.Feed.API.csproj"

COPY . .
WORKDIR "/src/ConnectSphere/src/ConnectSphere.Feed.API"
RUN dotnet build "ConnectSphere.Feed.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ConnectSphere.Feed.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectSphere.Feed.API.dll"]