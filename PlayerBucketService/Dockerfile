FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["PlayerBucketService/PlayerBucketService.csproj", "PlayerBucketService/"]
COPY ["PlayerBucket.Client/PlayerBucket.Client.csproj", "PlayerBucket.Client/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucketService.Core/PlayerBucketService.Core.csproj", "PlayerBucketService.Core/"]
COPY ["PlayerBucketService.Repositories/PlayerBucketService.Repositories.csproj", "PlayerBucketService.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
RUN dotnet restore "./PlayerBucketService/./PlayerBucketService.csproj"
COPY . .
WORKDIR "/src/PlayerBucketService"
RUN dotnet build "./PlayerBucketService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PlayerBucketService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayerBucketService.dll"]