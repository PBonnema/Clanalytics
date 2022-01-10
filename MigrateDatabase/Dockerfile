# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

###################
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS publish
ARG MigrateDatabaseDir
ARG DataAccessDir

WORKDIR /src/DataAccess
COPY ${DataAccessDir}/*.csproj ./
RUN dotnet restore "DataAccess.csproj"

WORKDIR /src/MigrateDatabase
COPY ${MigrateDatabaseDir}/*.csproj ./
RUN dotnet restore "MigrateDatabase.csproj"

COPY ${MigrateDatabaseDir}/ /src/MigrateDatabase/
COPY ${DataAccessDir}/ /src/DataAccess/
RUN dotnet publish --no-restore "MigrateDatabase.csproj" -c Release -o /app/publish

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "MigrateDatabase.dll"]
