FROM classtranscribe/dotnet_sdk_2.2:latest AS api_csproj
WORKDIR /src
COPY ["./ClassTranscribeServer/ClassTranscribeServer.csproj", ""]
COPY ["./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj", ""]
RUN dotnet restore "ClassTranscribeDatabase.csproj"
RUN dotnet restore "ClassTranscribeServer.csproj"

FROM api_csproj AS api_build
COPY ./vs_appsettings.json ./vs_appsettings.json
COPY ./world_universities_and_domains.json ./world_universities_and_domains.json
COPY ./ClassTranscribeServer ./ClassTranscribeServer
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
WORKDIR "/src/ClassTranscribeServer"
RUN dotnet build "ClassTranscribeServer.csproj" -c Debug -o /app

FROM api_build AS api_publish
RUN dotnet publish "ClassTranscribeServer.csproj" -c Debug -o /app
WORKDIR /


# Instructions for enabling ssh in a container (For root additionally google "PermitRootLogin SSH")
# RUN apt-get update
# RUN apt-get install -y openssh-server unzip build-essential gdbserver
# RUN mkdir /var/run/sshd
# RUN chmod 0755 /var/run/sshd
# RUN useradd --create-home --shell /bin/bash --groups sudo god
# RUN echo 'god:1989' | chpasswd
# RUN echo 'root:1989' | chpasswd