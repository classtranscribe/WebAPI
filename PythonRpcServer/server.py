from __future__ import print_function

import ct_pb2
import ct_pb2_grpc
import grpc
import time
import logging
from concurrent import futures
import scenedetector
from kaltura import Kaltura
import echo
import youtube
import ffmpeg

_ONE_DAY_IN_SECONDS = 60 * 60 * 24

class PythonServerServicer(ct_pb2_grpc.PythonServerServicer):
    def GetScenesRPC(self, request, context):
        res = scenedetector.find_scenes(request.filePath)
        return ct_pb2.JsonString(json = res)
    
    def GetKalturaChannelEntriesRPC(self, request, context):
        res = Kaltura().getKalturaChannelEntries(int(request.Url))
        return ct_pb2.JsonString(json = res)
    
    def DownloadKalturaVideoRPC(self, request, context):
        filePath, ext = Kaltura().downloadLecture(request.videoUrl)
        return ct_pb2.File(filePath = filePath, ext = ext)
        
    def GetEchoPlaylistRPC(self, request, context):
        res = echo.get_syllabus(request.Url, stream = request.Stream)
        return ct_pb2.JsonString(json = res)
    
    def DownloadEchoVideoRPC(self, request, context):
        filePath, ext = echo.downloadLecture(request.videoUrl, request.additionalInfo)
        return ct_pb2.File(filePath = filePath, ext = ext)
    
    def GetYoutubePlaylistRPC(self, request, context):
        res = youtube.get_youtube_playlist(request.Url)
        return ct_pb2.JsonString(json = res)
    
    def DownloadYoutubeVideoRPC(self, request, context):
        filePath, ext = youtube.download_youtube_video(request.videoUrl)
        return ct_pb2.File(filePath = filePath, ext = ext)
    
    def ConvertVideoToWavRPC(self, request, context):
        filePath, ext = ffmpeg.convertVideoToWav(request.filePath)
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