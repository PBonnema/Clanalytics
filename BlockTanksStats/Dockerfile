# syntax=docker/dockerfile:1.2
FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS base
WORKDIR /app

RUN apt-get update \
	# Taken from https://www.mono-project.com/download/stable/#download-lin. To ensure we get a recent version of libgdiplus
	&& apt-get install -y gnupg ca-certificates \
	&& apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
	&& echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
	&& apt-get update \
	&& apt-get install -y \
	# Based on https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#dependencies and https://github.com/dotnet/runtime/issues/24070
		libc6-dev \
	# Based on https://stackoverflow.com/questions/53693042/dllnotfoundexception-using-skiasharp-1-68-on-linux/55002182#55002182
		fontconfig \
	## libgdiplus was taken from https://github.com/dotnet/core/issues/2746 and https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#dependencies
	## as a fix for 'System.DllNotFoundException: Unable to load DLL 'libgdiplus': The specified module could not be found.'
	## that ClosedXML.Report is throwing
		libgdiplus

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

###################
FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS publish

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

RUN dotnet publish --no-restore "BlockTanksStats.csproj" --configuration Release --output /app/publish --no-self-contained --nologo

###################
FROM base AS final
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "BlockTanksStats.dll"]
