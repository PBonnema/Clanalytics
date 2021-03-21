# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine AS base
WORKDIR /app

###################
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
ARG IngestionDir
ARG DataAccessDir

WORKDIR /src/DataAccess
COPY ${DataAccessDir}/*.csproj ./
RUN dotnet restore "DataAccess.csproj"

WORKDIR /src/Ingestion
COPY ${IngestionDir}/*.csproj ./
RUN dotnet restore "Ingestion.csproj"

COPY ${IngestionDir}/ /src/Ingestion/
COPY ${DataAccessDir}/ /src/DataAccess/
RUN dotnet build --no-restore "Ingestion.csproj" -c Release -o /app/build

###################
FROM build AS publish
RUN dotnet publish --no-restore "Ingestion.csproj" -c Release -o /app/publish

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "Ingestion.dll"]
