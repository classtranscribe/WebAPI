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
import hasher 
import ffmpeg
import os
import traceback
from time import perf_counter 
# Main entry point for docker container

_ONE_DAY_IN_SECONDS = 60 * 60 * 24

def LogWorker(logId, worker):
    start_time = perf_counter()
    logger = lambda message : print(f"{logId}:{message}")
    try:
        logger("Starting...")
        result = worker()
        return result
    except Exception as e:
        logger(f"Exception {e}")
        traceback.print_exc()
        raise e
    finally:
        end_time = perf_counter()
        logger(f"Task returning after {int(end_time - start_time)} seconds.")


class PythonServerServicer(ct_pb2_grpc.PythonServerServicer):
    def GetScenesRPC(self, request, context):
        res = scenedetector.find_scenes(request.filePath)
        return ct_pb2.JsonString(json = res)

    def ToPhraseHintsRPC(self, request, context):
        res = videophrases.to_phrase_hints(request.rawPhraseData)
        return res
    
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

    # Todo Rename to ComputeFileHashRPC and update? or insert new entry in ct.proto
    def ComputeFileHash(self, request, context):
        hash = hasher.hashFile(request.file, request.algorithms)
        return ct_pb2.FileHashResponse(result = hash)

    def GetMediaInfoRPC(self, request, context):
        result = LogWorker(f"GetMediaInfo({request.filePath})", lambda: ffmpeg.getMediaInfo(request.filePath))
        return  ct_pb2.JsonString(json = result)

def serve():
    print("Python RPC Server Starting")
    
    # Until we can ensure no timeouts on remote services, the default here is set to a conservative low number
    # This is to ensure we can still make progress even if every python tasks tries to use all cpu cores.
    max_workers=int(os.getenv('NUM_PYTHON_WORKERS', 3))
    print(f"max_workers={max_workers}")

    server = grpc.server(futures.ThreadPoolExecutor(max_workers=max_workers))
    
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
