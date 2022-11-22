#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["PlayerBucketMigrateJob/PlayerBucketMigrateJob.csproj", "PlayerBucketMigrateJob/"]
COPY ["PlayerBucket.Client/PlayerBucket.Client.csproj", "PlayerBucket.Client/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
RUN dotnet restore "PlayerBucketMigrateJob/PlayerBucketMigrateJob.csproj"
COPY . .
WORKDIR "/src/PlayerBucketMigrateJob"
RUN dotnet build "PlayerBucketMigrateJob.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "PlayerBucketMigrateJob.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayerBucketMigrateJob.dll"]