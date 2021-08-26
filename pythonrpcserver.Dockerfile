
# Total laptop build 626 seconds
FROM python:3.7-slim-stretch

RUN apt-get update
RUN apt-get install -y curl gcc g++ make libglib2.0-0 libsm6 libxext6 libxrender-dev ffmpeg

# Build stuff for tesseract
# Based on https://medium.com/quantrium-tech/installing-tesseract-4-on-ubuntu-18-04-b6fcd0cbd78f
RUN apt-get install -y automake pkg-config libsdl-pango-dev libicu-dev libcairo2-dev bc libleptonica-dev
RUN  curl -L  https://github.com/tesseract-ocr/tesseract/archive/refs/tags/4.1.1.tar.gz  | tar xvz

WORKDIR /tesseract-4.1.1
RUN ./autogen.sh && ./configure && make -j && make install && ldconfig 
# Slow! The above line takes 435 seconds on my laptop
RUN make training && make training-install
# The above line takes 59 seconds on my laptop 

RUN curl -L -o tessdata/eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/master/eng.traineddata
RUN curl -L -o tessdata/osd.traineddata https://github.com/tesseract-ocr/tessdata/raw/master/osd.traineddata
ENV TESSDATA_PREFIX=/tesseract-4.1.1/tessdata
#Disable multi-threading
ENV OMP_THREAD_LIMIT=1

WORKDIR /PythonRpcServer

COPY ./PythonRpcServer/requirements.txt requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY ct.proto ct.proto
RUN python -m grpc_tools.protoc -I . --python_out=./ --grpc_python_out=./ ct.proto

COPY ./PythonRpcServer .

# Downloaded tgz from https://github.com/nficano/pytube and renamed to include version
RUN pip install --no-cache-dir pytube-v11.0.0.tar.gz

RUN python -m nltk.downloader stopwords brown


# Nice:Very low priority but not lowest priority (18 out of 19)
#ionice: Best effort class but second lowest priory (6 out of 7)
CMD [ "nice","-n","18", "ionice","-c","2","-n","6", "python3", "-u", "/PythonRpcServer/server.py" ]