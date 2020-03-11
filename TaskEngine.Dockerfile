FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic as build

WORKDIR /
RUN git clone https://github.com/eficode/wait-for.git

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
RUN apt-get install -y build-essential
RUN apt-get install -y libasound2 
RUN apt-get install -y wget libssl1.0.0
RUN apt-get -q update
RUN apt-get -qy install netcat

FROM publish_base as publish
WORKDIR /
COPY --from=build /wait-for .
WORKDIR /app
COPY --from=build /app .
CMD ["dotnet", "/app/TaskEngine.dll"]