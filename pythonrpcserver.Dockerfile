FROM python:3.7-slim-stretch

RUN apt-get update
RUN apt-get install -y gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg

WORKDIR /PythonRpcServer

COPY ./PythonRpcServer/requirements.txt requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY ct.proto ct.proto
RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto

COPY ./PythonRpcServer .

# Nice:Very low priority but not lowest priority (18 out of 19)
#ionice: Best effort class but second lowest priory (6 out of 7)
CMD [ "nice","-n","18", "ionice","-c","2","-n","6", "python3", "-u", "/PythonRpcServer/server.py" ]