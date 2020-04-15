FROM python:3.7-slim-stretch

RUN apt-get update
RUN apt-get install -y gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg

WORKDIR /PythonRpcServer

COPY ./PythonRpcServer/requirements.txt requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY ct.proto ct.proto
RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto

COPY ./PythonRpcServer .

CMD [ "python3", "-u", "/PythonRpcServer/server.py" ]