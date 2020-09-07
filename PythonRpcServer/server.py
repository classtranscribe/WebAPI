from __future__ import print_function

import ct_pb2
import ct_pb2_grpc
import grpc
import time
import logging
from concurrent import futures
import scenedetector
import echo
from youtube import YoutubeProvider
from echo import EchoProvider
from kaltura import KalturaProvider
from mediaprovider import InvalidPlaylistInfoException
import ffmpeg
import os
# Main entry point for docker container

_ONE_DAY_IN_SECONDS = 60 * 60 * 24

class PythonServerServicer(ct_pb2_grpc.PythonServerServicer):
    def GetScenesRPC(self, request, context):
        res = scenedetector.find_scenes(request.filePath)
        return ct_pb2.JsonString(json = res)
    
    def GetKalturaChannelEntriesRPC(self, request, context):
        kalturaprovider = KalturaProvider()
        try:
            res = kalturaprovider.getPlaylistItems(request)
            return ct_pb2.JsonString(json = res)
        except InvalidPlaylistInfoException as e:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details(e.message)
            return ct_pb2.JsonString()
    
    def DownloadKalturaVideoRPC(self, request, context):
        kalturaprovider = KalturaProvider()
        filePath, ext = kalturaprovider.getMedia(request)
        return ct_pb2.File(filePath = filePath, ext = ext)
        
    def GetEchoPlaylistRPC(self, request, context):
        echoprovider = EchoProvider()
        try:
            res = echoprovider.getPlaylistItems(request)
            return ct_pb2.JsonString(json = res)
        except InvalidPlaylistInfoException as e:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details(e.message)
            return ct_pb2.JsonString()
        
    
    def DownloadEchoVideoRPC(self, request, context):
        echoprovider = EchoProvider()
        filePath, ext = echoprovider.getMedia(request)
        return ct_pb2.File(filePath = filePath, ext = ext)
    
    def GetYoutubePlaylistRPC(self, request, context):
        youtubeprovider = YoutubeProvider()
        try:
            res = youtubeprovider.getPlaylistItems(request)
            return ct_pb2.JsonString(json = res)
        except InvalidPlaylistInfoException as e:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details(e.message)
            return ct_pb2.JsonString()
    
    def DownloadYoutubeVideoRPC(self, request, context):
        youtubeprovider = YoutubeProvider()
        filePath, ext = youtubeprovider.getMedia(request)
        return ct_pb2.File(filePath = filePath, ext = ext)

    def ConvertVideoToWavRPCWithOffset(self, request, context):
        filePath, ext = ffmpeg.convertVideoToWavWithOffset(request.file.filePath, request.offset)
        return ct_pb2.File(filePath = filePath, ext = ext)

    def ProcessVideoRPC(self, request, context):
        filePath, ext = ffmpeg.processVideo(request.filePath)
        return ct_pb2.File(filePath = filePath, ext = ext)

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
