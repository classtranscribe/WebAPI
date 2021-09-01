
# Python build split into 3 parts:
# i) C++ compilation of packages we cant just apt install (specifically Tesseract 4) (slow)
# ii) python pip install from requirements.txt and 3rd party python packages
# iii) build of source code (including gprc)
FROM python:3.7-slim-stretch  as python_package_compile

RUN apt-get update && apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg

# Build stuff for tesseract
# Based on https://medium.com/quantrium-tech/installing-tesseract-4-on-ubuntu-18-04-b6fcd0cbd78f
RUN apt-get install -y automake pkg-config libsdl-pango-dev libicu-dev libcairo2-dev bc libleptonica-dev
RUN  curl -L  https://github.com/tesseract-ocr/tesseract/archive/refs/tags/4.1.1.tar.gz  | tar xvz

WORKDIR /tesseract-4.1.1
RUN ./autogen.sh && ./configure && make -j && make install && ldconfig 
# Slow! The above line takes 435 seconds on my laptop
RUN make training && make training-install
# The above line takes 59 seconds on my laptop 

RUN curl -L -o tessdata/eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/master/eng.traineddata \
    && curl -L -o tessdata/osd.traineddata https://github.com/tesseract-ocr/tessdata/raw/master/osd.traineddata

COPY ./PythonRpcServer/thirdparty/ /thirdparty

FROM python_package_compile as python_publish_base

WORKDIR /PythonRpcServer
COPY ./PythonRpcServer/requirements.txt requirements.txt
# Downloaded zip of repo from https://github.com/nficano/pytube and renamed to include version
RUN python -m pip install --no-cache-dir /thirdparty/pytube-master-11.0.1.tar.gz && \
    python -m pip install --no-cache-dir -r requirements.txt && \
    python -m nltk.downloader stopwords brown


FROM python_publish_base as python_publish

ENV TESSDATA_PREFIX=/tesseract-4.1.1/tessdata
#Disable multi-threading
ENV OMP_THREAD_LIMIT=1

WORKDIR /PythonRpcServer

COPY ct.proto ct.proto
COPY ./PythonRpcServer/src .

RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto

# Nice:Very low priority but not lowest priority (18 out of 19)
#ionice: Best effort class but second lowest priory (6 out of 7)
CMD [ "nice","-n","18", "ionice","-c","2","-n","6", "python3", "-u", "/PythonRpcServer/server.py" ]