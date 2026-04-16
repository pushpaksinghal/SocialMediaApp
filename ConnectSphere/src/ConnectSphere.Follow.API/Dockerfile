FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ConnectSphere/src/ConnectSphere.Follow.API/ConnectSphere.Follow.API.csproj", "ConnectSphere/src/ConnectSphere.Follow.API/"]
RUN dotnet restore "ConnectSphere/src/ConnectSphere.Follow.API/ConnectSphere.Follow.API.csproj"

COPY . .
WORKDIR "/src/ConnectSphere/src/ConnectSphere.Follow.API"
RUN dotnet build "ConnectSphere.Follow.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ConnectSphere.Follow.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectSphere.Follow.API.dll"]