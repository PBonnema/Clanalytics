# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine AS base
WORKDIR /app

# Install cultures (same approach as Alpine SDK image)
RUN apk add --no-cache icu-libs

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

###################
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS publish
ARG BlockTanksStatsDir
ARG DataAccessDir

WORKDIR /src/DataAccess
COPY ${DataAccessDir}/*.csproj ./
RUN dotnet restore "DataAccess.csproj"

WORKDIR /src/BlockTanksStats
COPY ${BlockTanksStatsDir}/*.csproj ./
RUN dotnet restore "BlockTanksStats.csproj"

COPY ${BlockTanksStatsDir}/ /src/BlockTanksStats/
COPY ${DataAccessDir}/ /src/DataAccess/
RUN dotnet publish --no-restore "BlockTanksStats.csproj" -c Release -o /app/publish

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "BlockTanksStats.dll"]
