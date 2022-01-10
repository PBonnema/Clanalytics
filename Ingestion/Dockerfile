# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

###################
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS publish

# First copy ONLY the csproj file and restore and then copy the rest of the files.
# This has better caching behaviour.

ARG DataAccessDir
WORKDIR /src/DataAccess
COPY ${DataAccessDir}/*.csproj ./
RUN dotnet restore "DataAccess.csproj"

ARG IngestionDir
WORKDIR /src/Ingestion
COPY ${IngestionDir}/*.csproj ./
RUN dotnet restore "Ingestion.csproj"

# Now copy the rest of the files and build
COPY ${IngestionDir}/ /src/Ingestion/
COPY ${DataAccessDir}/ /src/DataAccess/
RUN dotnet publish --no-restore "Ingestion.csproj" -c Release -o /app/publish

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "Ingestion.dll"]
