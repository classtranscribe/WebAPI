from __future__ import print_function

import ct_pb2
import ct_pb2_grpc
import grpc
import time
import logging
from concurrent import futures
import scenedetector
from kaltura import Kaltura


_ONE_DAY_IN_SECONDS = 60 * 60 * 24

class PythonServerServicer(ct_pb2_grpc.PythonServerServicer):
    def GetScenesRPC(self, request, context):
        res = scenedetector.find_scenes(request.filePath)
        return ct_pb2.JsonString(json = res)
    
    def GetKalturaChannelEntriesRPC(self, request, context):
        print(request)
        res = Kaltura().getKalturaChannelEntries(int(request.Url))
        return ct_pb2.JsonString(json = res)
    


def serve():
    print("Python RPC Server Starting")
    
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    ct_pb2_grpc.add_PythonServerServicer_to_server(
        PythonServerServicer(), server)
    server.add_insecure_port('[::]:50051')
    server.start()
    print("Python RPC Server Started")
    try:
        while True:
            time.sleep(_ONE_DAY_IN_SECONDS)
    except KeyboardInterrupt:
        server.stop(0)
    print("Python RPC Server Stopped")

if __name__ == '__main__':
    logging.basicConfig()
    serve()    