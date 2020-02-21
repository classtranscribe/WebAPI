FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic as build
WORKDIR /src

COPY ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj
RUN dotnet restore ./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj

COPY ./ClassTranscribeServer/ClassTranscribeServer.csproj ./ClassTranscribeServer/ClassTranscribeServer.csproj
RUN dotnet restore ./ClassTranscribeServer/ClassTranscribeServer.csproj

COPY ./vs_appsettings.json ./vs_appsettings.json
COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ClassTranscribeServer ./ClassTranscribeServer
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
WORKDIR /src/ClassTranscribeServer

RUN dotnet publish ClassTranscribeServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-bionic as publish
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443
ENTRYPOINT dotnet /app/ClassTranscribeServer.dll