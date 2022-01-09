# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine AS base
WORKDIR /app

RUN apk update && apk add --no-cache \
# Install cultures (same approach as Alpine SDK image)
	icu-libs \
# libgdiplus was taken from https://github.com/dotnet/core/issues/2746 and added the --repository according to https://docs.microsoft.com/en-us/dotnet/core/install/linux-alpine#dependencies
# as a fix for 'System.DllNotFoundException: Unable to load DLL 'libgdiplus': The specified module could not be found.'
# that ClosedXML.Report is throwing
	libgdiplus \
	--repository https://dl-3.alpinelinux.org/alpine/edge/testing/ \
# ttf-dejavu was taken from https://github.com/AdoptOpenJDK/openjdk-docker/issues/529#issuecomment-811249458 to fix this exception: System.ArgumentException: Font '?' cannot be found.
	ttf-dejavu \
# Taken from https://stackoverflow.com/a/55002182 and adapted with https://github.com/docker-library/php/issues/225
	freetype-dev fontconfig fontconfig-dev libjpeg-turbo-dev libpng-dev
	#libfontconfig1

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

###################
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS publish

# First copy ONLY the csproj file and restore and then copy the rest of the files.
# This has better caching behaviour.

ARG DataAccessDir
WORKDIR /src/DataAccess/
COPY ${DataAccessDir}/*.csproj ./
RUN dotnet restore "DataAccess.csproj"

ARG BlockTanksStatsDir
WORKDIR /src/BlockTanksStats/
COPY ${BlockTanksStatsDir}/*.csproj ./
RUN dotnet restore "BlockTanksStats.csproj"

# Now copy the rest of the files and build
COPY ${DataAccessDir}/ /src/DataAccess/
COPY ${BlockTanksStatsDir}/ /src/BlockTanksStats/

RUN dotnet publish --no-restore "BlockTanksStats.csproj" -c Release -o /app/publish

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "BlockTanksStats.dll"]
