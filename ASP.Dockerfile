#FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443

FROM ubuntu:18.04 AS build
RUN apt-get update
RUN apt-get install -y wget nano
RUN apt-get install -y software-properties-common
RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get -y update
RUN add-apt-repository universe
RUN apt-get update
RUN apt-get install -y apt-transport-https
# Transcription things
RUN apt-get install -y liblttng-ust0 libcurl3 libssl1.0.0 libkrb5-3 zlib1g libicu60 libasound2
RUN apt update
#RUN apt install -y snapd

ENV BIN_PATH "/usr/bin"
RUN wget -q https://download.visualstudio.microsoft.com/download/pr/72ce4d40-9063-4a2e-a962-0bf2574f75d1/5463bb92cff4f9c76935838d1efbc757/dotnet-sdk-3.0.100-preview6-012264-linux-x64.tar.gz
RUN wget -q https://download.visualstudio.microsoft.com/download/pr/3224f4c4-8333-4b78-b357-144f7d575ce5/ce8cb4b466bba08d7554fe0900ddc9dd/dotnet-sdk-2.2.301-linux-x64.tar.gz
RUN mkdir -p $BIN_PATH/dotnet && tar zxf dotnet-sdk-3.0.100-preview6-012264-linux-x64.tar.gz -C $BIN_PATH/dotnet
RUN mkdir -p $BIN_PATH/dotnet && tar zxf dotnet-sdk-2.2.301-linux-x64.tar.gz -C $BIN_PATH/dotnet


# RUN apt-get install -y aspnetcore-runtime-3.0
#RUN apt-get install -y dotnet-sdk-2.2
RUN apt-get install -y screen

FROM build AS publish
#RUN dotnet publish "ClassTranscribeServer.csproj" -c Release -o /app
WORKDIR /src
COPY ["./ClassTranscribeServer/ClassTranscribeServer.csproj", ""]
COPY ["./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj", ""]
COPY ["./TaskEngine/TaskEngine.csproj", ""]
#EXPOSE 80
#EXPOSE 443

ENV DOTNET_ROOT $BIN_PATH/dotnet
ENV PATH $PATH:$BIN_PATH/dotnet/

RUN dotnet restore "ClassTranscribeServer.csproj"
RUN dotnet restore "ClassTranscribeDatabase.csproj"
RUN dotnet restore "TaskEngine.csproj"
#COPY . .
WORKDIR /
#RUN dotnet build "ClassTranscribeServer.csproj" -c Release -o /app
#ENTRYPOINT ["dotnet", "ClassTranscribeServer.dll"]

#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "ClassTranscribeServer.dll"]