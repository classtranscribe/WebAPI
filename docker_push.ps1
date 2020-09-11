docker build -f .\pythonrpcserver.Dockerfile -t classtranscribe/pythonrpcserver:$1 .
docker push classtranscribe/pythonrpcserver:$1
docker build -f .\API.Dockerfile -t classtranscribe/api:$1 .
docker push classtranscribe/api:$1
docker build -f .\TaskEngine.Dockerfile -t classtranscribe/taskengine:$1 .
docker push classtranscribe/taskengine:$1
