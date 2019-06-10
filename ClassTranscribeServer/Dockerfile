#FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443

FROM ubuntu:18.04 AS build
RUN apt-get update
RUN apt-get install -y wget
RUN apt-get install -y software-properties-common
RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get -y update
RUN add-apt-repository universe
RUN apt-get update
RUN apt-get install -y apt-transport-https
RUN apt-get install -y aspnetcore-runtime-2.2
RUN apt-get install -y dotnet-sdk-2.2

FROM build AS publish
#RUN dotnet publish "ClassTranscribeServer.csproj" -c Release -o /app
WORKDIR /src
COPY ["ClassTranscribeServer.csproj", ""]
#EXPOSE 80
#EXPOSE 443
RUN dotnet restore "ClassTranscribeServer.csproj"
#COPY . .
WORKDIR /
#RUN dotnet build "ClassTranscribeServer.csproj" -c Release -o /app
#ENTRYPOINT ["dotnet", "ClassTranscribeServer.dll"]

#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "ClassTranscribeServer.dll"]