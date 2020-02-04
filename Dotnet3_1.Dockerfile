FROM classtranscribe/dotnet_base:latest AS dotnet_sdk_3.1
RUN apt-get install -y dotnet-sdk-3.1