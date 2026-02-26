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


FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
# See https://mcr.microsoft.com/en-us/product/dotnet/sdk/tags

# Running the AMD64 version is of the SDK is broken
# https://github.com/dotnet/dotnet-docker/discussions/4285
# https://github.com/NuGet/Home/issues/13062

RUN apt-get -q update && apt-get -qy install git
WORKDIR /
RUN git clone https://github.com/eficode/wait-for.git

WORKDIR /src
COPY ./Directory.Build.props ./
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS publish_base

# Install libasound2t64 and create a symlink to map it to the legacy filename expected by the Speech SDK.
# This ensures native binaries searching for libasound.so.2 can resolve the dependency on Ubuntu 24.04.
RUN apt-get -q update && \
    apt-get install -y libasound2t64 netcat-traditional && \
    ln -s /usr/lib/x86_64-linux-gnu/libasound.so.2 /usr/lib/libasound.so.2 && \
    apt-get -q update

FROM publish_base AS publish
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
