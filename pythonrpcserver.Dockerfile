# # ------------------------------
# # Stage 1: Build Whisper.cpp
# # ------------------------------
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
    cmake -B build -DWHISPER_BUILD_EXAMPLES=ON && \
    cmake --build build --parallel $(nproc)
RUN bash ./models/download-ggml-model.sh base.en
RUN bash ./models/download-ggml-model.sh tiny.en
RUN bash ./models/download-ggml-model.sh large-v3
