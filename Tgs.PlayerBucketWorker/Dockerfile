#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.PlayerBucketWorker/Tgs.PlayerBucketWorker.csproj", "Tgs.PlayerBucketWorker/"]
COPY ["PlayerBucket.Client/PlayerBucket.Client.csproj", "PlayerBucket.Client/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucket.Repositories/PlayerBucket.Repositories.csproj", "PlayerBucket.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
COPY ["PlayerBucket.Worker.Core/PlayerBucket.Worker.Core.csproj", "PlayerBucket.Worker.Core/"]
RUN dotnet restore "./Tgs.PlayerBucketWorker/./Tgs.PlayerBucketWorker.csproj"
COPY . .
WORKDIR "/src/Tgs.PlayerBucketWorker"
RUN dotnet build "./Tgs.PlayerBucketWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.PlayerBucketWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.PlayerBucketWorker.dll"]