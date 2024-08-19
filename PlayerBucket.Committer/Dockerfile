#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["PlayerBucket.Committer/PlayerBucket.Committer.csproj", "PlayerBucket.Committer/"]
COPY ["PlayerBucket.Committer.Core/PlayerBucket.Committer.Core.csproj", "PlayerBucket.Committer.Core/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucket.Committer.Repositories/PlayerBucket.Committer.Repositories.csproj", "PlayerBucket.Committer.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
RUN dotnet restore "./PlayerBucket.Committer/./PlayerBucket.Committer.csproj"
COPY . .
WORKDIR "/src/PlayerBucket.Committer"
RUN dotnet build "./PlayerBucket.Committer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PlayerBucket.Committer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayerBucket.Committer.dll"]