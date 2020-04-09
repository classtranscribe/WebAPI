docker build -f .\rpcserver.Dockerfile -t classtranscribe/rpcserver:$1 .
docker push classtranscribe/rpcserver:$1
docker build -f .\API.Dockerfile -t classtranscribe/api:$1 .
docker push classtranscribe/api:$1
docker build -f .\TaskEngine.Dockerfile -t classtranscribe/taskengine:$1 .
docker push classtranscribe/taskengine:$1
