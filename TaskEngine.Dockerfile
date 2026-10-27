#FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim as build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
# See https://mcr.microsoft.com/en-us/product/dotnet/sdk/tags
#See more comments in API.Dockerfile
# RUN ls
RUN dotnet --list-sdks

WORKDIR /
RUN git clone https://github.com/eficode/wait-for.git

WORKDIR /src
COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj
# --verbosity normal|diagnostic


RUN dotnet restore --verbosity diagnostic ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj

COPY ./TaskEngine/TaskEngine.csproj ./TaskEngine/TaskEngine.csproj
RUN dotnet restore ./TaskEngine/TaskEngine.csproj

COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ct.proto ./ct.proto
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./TaskEngine ./TaskEngine
WORKDIR /src/TaskEngine
RUN dotnet publish TaskEngine.csproj -c Release -o /app --no-restore

#FROM mcr.microsoft.com/dotnet/aspnet:8.0 as publish_base
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS publish_base
# https://hub.docker.com/_/microsoft-dotnet-aspnet/

# Install prerequisites for Azure Speech Services: build-essential libssl-dev ca-certificates libasound2 wget
# See https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstarts/setup-platform
# 10.0 libasound2 replaced with libasound2t64

RUN apt-get update && apt-get install -y  build-essential libssl-dev ca-certificates libasound2t64 wget && \
apt-get install -y netcat-traditional && apt-get -q update

# Microsoft 8.0 issue: https://github.com/Azure-Samples/cognitive-services-speech-sdk/issues/2204
# Previously we installed OpenSSL 1.1.1 here because older Speech SDKs needed it.
# Speech SDK 1.53+ works with the OpenSSL 3 that ships with Ubuntu 24.04 (noble), so the hack is no longer needed.
# RUN wget http://archive.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.0g-2ubuntu4_amd64.deb
# RUN dpkg -i libssl1.1_1.1.0g-2ubuntu4_amd64.deb


FROM publish_base AS publish
WORKDIR /
COPY --from=build /wait-for .
WORKDIR /app
COPY --from=build /app .
CMD ["dotnet", "/app/TaskEngine.dll"]
