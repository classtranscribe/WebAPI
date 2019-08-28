FROM classtranscribe/dotnet_sdk_3.0:latest AS taskengine_csproj
WORKDIR /src
COPY ["./ClassTranscribeDatabase/ClassTranscribeDatabase.csproj", ""]
COPY ["./TaskEngine/TaskEngine.csproj", ""]
RUN dotnet restore "ClassTranscribeDatabase.csproj"
RUN dotnet restore "TaskEngine.csproj"

FROM taskengine_csproj AS taskengine_build
COPY ./TaskEngine ./TaskEngine
COPY ./ClassTranscribeDatabase ./ClassTranscribeDatabase
COPY ./vs_appsettings.json ./vs_appsettings.json
COPY ./ct.proto ./ct.proto
WORKDIR "/src/TaskEngine"
RUN dotnet build "TaskEngine.csproj" -c Debug -o /app

FROM taskengine_build AS taskengine_publish
RUN dotnet publish "TaskEngine.csproj" -c Debug -o /app
WORKDIR /
