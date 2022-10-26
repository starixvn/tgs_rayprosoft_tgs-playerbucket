#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.PlayerBucketWorker/Tgs.PlayerBucketWorker.csproj", "Tgs.PlayerBucketWorker/"]
COPY ["PlayerBucket.Client/PlayerBucket.Client.csproj", "PlayerBucket.Client/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucket.Repositories/PlayerBucket.Repositories.csproj", "PlayerBucket.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
COPY ["PlayerBucket.Worker.Core/PlayerBucket.Worker.Core.csproj", "PlayerBucket.Worker.Core/"]
RUN dotnet restore "Tgs.PlayerBucketWorker/Tgs.PlayerBucketWorker.csproj"
COPY . .
WORKDIR "/src/Tgs.PlayerBucketWorker"
RUN dotnet build "Tgs.PlayerBucketWorker.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.PlayerBucketWorker.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.PlayerBucketWorker.dll"]