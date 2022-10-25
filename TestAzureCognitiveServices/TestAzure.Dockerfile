FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-bionic as build1

WORKDIR /src/TestAzureCognitiveServices

COPY . .
RUN dotnet restore ./TestAzureCognitiveServices.csproj
RUN dotnet publish ./TestAzureCognitiveServices.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1.3-bionic as publish_base1
#RUN apt-get -q update

FROM publish_base1 as publish1
WORKDIR /app
COPY --from=build1 /app .
COPY shortwav.wav /
CMD ["dotnet", "/app/TestAzureCognitiveServices.dll"]

# Example
#docker build -t azuretest -f TestAzure.Dockerfile .
#docker run -t azuretest ls