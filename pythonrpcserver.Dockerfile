# # ------------------------------
# # Stage 1: Build Whisper.cpp
# # ------------------------------
    FROM --platform=linux/amd64 python:3.8.15-slim-buster AS whisperbuild
    RUN apt-get update && \
        apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg git
    
    WORKDIR /whisper.cpp
    RUN git clone https://github.com/ggerganov/whisper.cpp . && make
    RUN bash ./models/download-ggml-model.sh base.en
    
# ------------------------------
# Stage 2: Setup Python RPC Server
# ------------------------------
    FROM --platform=linux/amd64 python:3.8.15-slim-buster AS rpcserver
    RUN apt-get update && \
        apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg
    
    ENV OMP_THREAD_LIMIT=1
    COPY --from=whisperbuild /whisper.cpp/main /usr/local/bin/whisper
    COPY --from=whisperbuild /whisper.cpp/models/ggml-base.en.bin /usr/local/bin/models/ggml-base.en.bin
    WORKDIR /PythonRpcServer
    
    COPY ./PythonRpcServer/requirements.txt requirements.txt
    RUN pip install --no-cache-dir --upgrade pip && \
        pip install --no-cache-dir -r requirements.txt
    
    COPY ct.proto ct.proto
    RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto
    
    COPY ./PythonRpcServer .
    
    CMD [ "nice", "-n", "18", "ionice", "-c", "2", "-n", "6", "python3", "-u", "/PythonRpcServer/server.py" ]
    
    
    