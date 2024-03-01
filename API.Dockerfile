# Also remove platform from docker-compose.override.yml for api and taskengine
# Uncomment build context in docker-compose.override.yml for api and taskengine

# e.g.,
#   taskengine:
#    image: classtranscribe/taskengine:staging
#    #xx platform: linux/amd64 # Nope - Causes SDK "dotnet restore" to hang on M1 Mac
#    build:
#      context: ../../WebAPI
#      target: publish
#      dockerfile: ./TaskEngine.Dockerfile
#


#FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 as build
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim as build
# See https://mcr.microsoft.com/en-us/product/dotnet/sdk/tags

# Running the AMD64 version is of the SDK is broken
# https://github.com/dotnet/dotnet-docker/discussions/4285
# https://github.com/NuGet/Home/issues/13062

RUN apt-get -q update && apt-get -qy install git
WORKDIR /
RUN git clone https://github.com/eficode/wait-for.git

WORKDIR /src
COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj
# Did not help ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
# Add --verbosity normal|diagnostic
RUN dotnet  --list-sdks 
RUN dotnet restore --verbosity diagnostic  ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj

COPY ./ClassTranscribeServer/ClassTranscribeServer.csproj ./ClassTranscribeServer/ClassTranscribeServer.csproj
RUN dotnet restore ./ClassTranscribeServer/ClassTranscribeServer.csproj

COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ct.proto ./ct.proto
COPY ./ClassTranscribeServer ./ClassTranscribeServer
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
WORKDIR /src/ClassTranscribeServer
RUN dotnet publish ClassTranscribeServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim as publish_base
# FROM mcr.microsoft.com/dotnet/aspnet:7.0.14-bookworm-slim as publish_base

# FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.3-bionic as publish_base
RUN apt-get -q update && apt-get -qy install netcat-traditional

FROM publish_base as publish
WORKDIR /
COPY --from=build /wait-for .
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443

ARG GITSHA1=unspecified
ENV GITSHA1=$GITSHA1

ARG BUILDNUMBER=unspecified
ENV BUILDNUMBER=$BUILDNUMBER

LABEL git_commit_hash=$GITSHA1

CMD ["dotnet", "/app/ClassTranscribeServer.dll"]