FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-bookworm-slim as build1

WORKDIR /src/TestAzureCognitiveServices

COPY . .
RUN dotnet restore ./TestAzureCognitiveServices.csproj
RUN dotnet publish ./TestAzureCognitiveServices.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim as publish_base1

# Grrr AzureServices does not work on dotnet8 on Debian 12 because it wont link to libssl3 - fix below is needed for short-term

# Install prerequisites for Azure Speech Services
# See https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstarts/setup-platform
RUN apt-get update
RUN apt-get -y install build-essential libssl-dev libasound2 wget

# Microsoft 8.0 issue: https://github.com/Azure-Samples/cognitive-services-speech-sdk/issues/2204
# This  will install OpenSSL 1.1.1 because it is needed by the Speech SDK.
RUN \
wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2.20_amd64.deb && \
wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/libssl-dev_1.1.1f-1ubuntu2.20_amd64.deb && \
dpkg -i libssl1.1_1.1.1f-1ubuntu2.20_amd64.deb && \
dpkg -i libssl-dev_1.1.1f-1ubuntu2.20_amd64.deb && \
rm libssl1.1_1.1.1f-1ubuntu2.20_amd64.deb libssl-dev_1.1.1f-1ubuntu2.20_amd64.deb





FROM publish_base1 as publish1
WORKDIR /app
COPY --from=build1 /app .
COPY shortwav.wav /
CMD ["dotnet", "/app/TestAzureCognitiveServices.dll"]

# Example
#docker build -t azuretest -f TestAzure.Dockerfile .
#docker run -t azuretest ls
# [690371]: 31ms SPX_TRACE_ERROR: AZ_LOG_ERROR:  shim_openssl.c:55 libssl could not be loaded
# [690371]: 31ms SPX_TRACE_ERROR: AZ_LOG_ERROR:  tlsio_openssl.c:2175 Could not load libssl