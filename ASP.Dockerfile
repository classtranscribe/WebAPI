FROM classtranscribe/dotnet_sdk_2.2:latest AS api_csproj
WORKDIR /src
COPY ["./ClassTranscribeServer/ClassTranscribeServer.csproj", ""]
COPY ["./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj", ""]
RUN dotnet restore "ClassTranscribeDatabase.csproj"
RUN dotnet restore "ClassTranscribeServer.csproj"


FROM classtranscribe/dotnet_sdk_3.0:latest AS taskengine_csproj
WORKDIR /src
COPY ["./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj", ""]
COPY ["./TaskEngine/TaskEngine.csproj", ""]
RUN dotnet restore "ClassTranscribeDatabase.csproj"
RUN dotnet restore "TaskEngine.csproj"


FROM api_csproj AS api_build
COPY ./ClassTranscribeServer ./ClassTranscribeServer
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./vs_appsettings.json ./vs_appsettings.json
WORKDIR "/src/ClassTranscribeServer"
RUN dotnet build "ClassTranscribeServer.csproj" -c Release -o /app

FROM api_build AS api_publish
RUN dotnet publish "ClassTranscribeServer.csproj" -c Release -o /app
WORKDIR /

FROM taskengine_csproj AS taskengine_build
COPY ./TaskEngine ./TaskEngine
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./vs_appsettings.json ./vs_appsettings.json
COPY ./ct.proto ./ct.proto
WORKDIR "/src/TaskEngine"
RUN dotnet build "TaskEngine.csproj" -c Release -o /app

FROM taskengine_build AS taskengine_publish
RUN dotnet publish "TaskEngine.csproj" -c Release -o /app
WORKDIR /
