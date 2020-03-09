FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic as build
WORKDIR /src

COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj
RUN dotnet restore ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj

COPY ./TaskEngine/TaskEngine.csproj ./TaskEngine/TaskEngine.csproj
RUN dotnet restore ./TaskEngine/TaskEngine.csproj

COPY ./vs_appsettings.json ./vs_appsettings.json
COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ct.proto ./ct.proto
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./TaskEngine ./TaskEngine
WORKDIR /src/TaskEngine
RUN dotnet publish TaskEngine.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-bionic as publish_base
RUN apt-get update
RUN apt-get install -y build-essential libasound2 wget libssl1.0.0

FROM publish_base as publish
RUN apt-get -qy install netcat
WORKDIR /app
COPY --from=build /app .
CMD ["dotnet", "/app/TaskEngine.dll"]