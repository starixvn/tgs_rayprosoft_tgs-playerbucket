#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["PlayerBucket.Committer/PlayerBucket.Committer.csproj", "PlayerBucket.Committer/"]
COPY ["PlayerBucket.Committer.Core/PlayerBucket.Committer.Core.csproj", "PlayerBucket.Committer.Core/"]
COPY ["PlayerBucket.Common/PlayerBucket.Common.csproj", "PlayerBucket.Common/"]
COPY ["PlayerBucket.Committer.Repositories/PlayerBucket.Committer.Repositories.csproj", "PlayerBucket.Committer.Repositories/"]
COPY ["PlayerBucket.DatabaseAccess/PlayerBucket.DatabaseAccess.csproj", "PlayerBucket.DatabaseAccess/"]
RUN dotnet restore "PlayerBucket.Committer/PlayerBucket.Committer.csproj"
COPY . .
WORKDIR "/src/PlayerBucket.Committer"
RUN dotnet build "PlayerBucket.Committer.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "PlayerBucket.Committer.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayerBucket.Committer.dll"]