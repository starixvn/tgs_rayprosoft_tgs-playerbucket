#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["PlayerBucketService/PlayerBucketService.csproj", "PlayerBucketService/"]
COPY ["PlayerBucketService.Core/PlayerBucketService.Core.csproj", "PlayerBucketService.Core/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucketService.Repositories/PlayerBucketService.Repositories.csproj", "PlayerBucketService.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
RUN dotnet restore "PlayerBucketService/PlayerBucketService.csproj"
COPY . .
WORKDIR "/src/PlayerBucketService"
RUN dotnet build "PlayerBucketService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "PlayerBucketService.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayerBucketService.dll"]