ARG RPCBASE="staging"
FROM classtranscribe/rpcbase:${RPCBASE}

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
