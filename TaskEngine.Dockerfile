FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-bookworm-slim as build
# See https://mcr.microsoft.com/en-us/product/dotnet/sdk/tags
# 7.0.404-1 as build
# FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-bionic as build

WORKDIR /
RUN git clone https://github.com/eficode/wait-for.git

WORKDIR /src
COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj
RUN dotnet restore ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj

COPY ./TaskEngine/TaskEngine.csproj ./TaskEngine/TaskEngine.csproj
RUN dotnet restore ./TaskEngine/TaskEngine.csproj

COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ct.proto ./ct.proto
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./TaskEngine ./TaskEngine
WORKDIR /src/TaskEngine
RUN dotnet publish TaskEngine.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim as publish_base
# FROM mcr.microsoft.com/dotnet/core/runtime:3.1.3-bionic as publish_base
RUN apt-get update && apt-get install -y build-essential libasound2 wget netcat-traditional && apt-get -q update

FROM publish_base as publish
WORKDIR /
COPY --from=build /wait-for .
WORKDIR /app
COPY --from=build /app .
CMD ["dotnet", "/app/TaskEngine.dll"]