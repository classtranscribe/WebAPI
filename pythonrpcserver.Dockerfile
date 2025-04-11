# ------------------------------
# Stage 1: Build Whisper.cpp
# ------------------------------
FROM --platform=linux/amd64 python:3.8.15-slim-buster AS whisperbuild
RUN apt-get update && \
    apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg git && \
    apt-get install -y wget && \
    wget https://github.com/Kitware/CMake/releases/download/v3.27.7/cmake-3.27.7-linux-x86_64.sh -O /tmp/cmake-install.sh && \
    chmod +x /tmp/cmake-install.sh && \
    /tmp/cmake-install.sh --skip-license --prefix=/usr/local && \
    rm /tmp/cmake-install.sh

WORKDIR /whisper.cpp
RUN git clone https://github.com/ggml-org/whisper.cpp . && \
    cmake -B build -DWHISPER_BUILD_EXAMPLES=ON -DBUILD_SHARED_LIBS=OFF && \
    cmake --build build --parallel $(nproc)
RUN bash ./models/download-ggml-model.sh base.en
RUN bash ./models/download-ggml-model.sh tiny.en
RUN bash ./models/download-ggml-model.sh large-v3

# ------------------------------
# Stage 2: Setup Python RPC Server
# ------------------------------
FROM --platform=linux/amd64 python:3.8.15-slim-buster AS rpcserver
RUN apt-get update && \
    apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg

ENV OMP_THREAD_LIMIT=1
COPY --from=whisperbuild /whisper.cpp/build/bin/whisper-cli /usr/local/bin/whisper
COPY --from=whisperbuild /whisper.cpp/models /PythonRpcServer/models
WORKDIR /PythonRpcServer

COPY ./PythonRpcServer/transcribe_hellohellohello.wav .
RUN whisper -ojf -f transcribe_hellohellohello.wav

COPY ./PythonRpcServer/requirements.txt requirements.txt
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

COPY ct.proto ct.proto
RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto

COPY ./PythonRpcServer .

CMD [ "nice", "-n", "18", "ionice", "-c", "2", "-n", "6", "python3", "-u", "/PythonRpcServer/server.py" ]
