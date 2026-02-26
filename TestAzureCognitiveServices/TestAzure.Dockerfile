FROM mcr.microsoft.com/dotnet/sdk:10.0-noble as build1

WORKDIR /src
COPY ./Directory.Build.props ./
COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/
WORKDIR /src/TestAzureCognitiveServices
COPY ./TestAzureCognitiveServices/TestAzureCognitiveServices.csproj ./
RUN dotnet restore ./TestAzureCognitiveServices.csproj

WORKDIR /src
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./TestAzureCognitiveServices ./TestAzureCognitiveServices
WORKDIR /src/TestAzureCognitiveServices
RUN dotnet publish ./TestAzureCognitiveServices.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble as publish_base1

# Install libasound2t64 and create a symlink to map it to the legacy filename expected by the Speech SDK.
# This ensures native binaries searching for libasound.so.2 can resolve the dependency on Ubuntu 24.04.
RUN apt-get update && \
    apt-get install -y libasound2t64 build-essential libssl-dev wget && \
    ln -s /usr/lib/x86_64-linux-gnu/libasound.so.2 /usr/lib/libasound.so.2 && \
    apt-get -q update

# Microsoft 8.0 issue: https://github.com/Azure-Samples/cognitive-services-speech-sdk/issues/2204
# This  will install OpenSSL 1.1.1 because it is needed by the Speech SDK.
COPY ./TestAzureCognitiveServices/install-libssl1.sh /
RUN /install-libssl1.sh

FROM publish_base1 as publish1
WORKDIR /app
COPY --from=build1 /app .
COPY ./TestAzureCognitiveServices/shortwav.wav /
CMD ["dotnet", "/app/TestAzureCognitiveServices.dll"]

# Example
#docker build -t azuretest -f TestAzure.Dockerfile .
#docker run -t azuretest ls
# [690371]: 31ms SPX_TRACE_ERROR: AZ_LOG_ERROR:  shim_openssl.c:55 libssl could not be loaded
# [690371]: 31ms SPX_TRACE_ERROR: AZ_LOG_ERROR:  tlsio_openssl.c:2175 Could not load libssl