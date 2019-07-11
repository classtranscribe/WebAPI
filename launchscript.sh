dotnet build /ClassTranscribeServer/ClassTranscribeServer.csproj -c Release -o /app/ClassTranscribeServer
dotnet build /TaskEngine/TaskEngine.csproj -c Release -o /app/TaskEngine
screen -d -m dotnet /app/ClassTranscribeServer/ClassTranscribeServer.dll &&
dotnet /app/TaskEngine/TaskEngine.dll