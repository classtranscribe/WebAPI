from ffmpy import FFmpeg
from time import perf_counter 

import utils

default_max_threads = 3

def convertVideoToWavWithOffset(input_filepath, offset):
    try:
        start_time = perf_counter()
        if offset is None:
            offset = 0.0
        
        nthreads = utils.getMaxThreads()
        
        print(f"convertVideoToWavWithOffset('{input_filepath}',{offset}) using {nthreads} thread(s).")
        output_filepath = utils.getTmpFile()
        # For less verbosity try, global_options= '-hide_banner -loglevel error -nostats'
        # See https://github.com/Ch00k/ffmpy/blob/master/ffmpy.py

        ext = '.wav'
        ff = FFmpeg(
            global_options=f"-hide_banner -loglevel error -nostats -threads {nthreads}",
            inputs={
                input_filepath: '-ss {}'.format(offset)},
            outputs={output_filepath: '-c:a pcm_s16le -ac 1 -y -ar 16000 -f wav'}
        )
        print(f"Starting. Audio output will be saved in {output_filepath}")
        ff.run()
        end_time = perf_counter()
        print(f"convertVideoToWavWithOffset('{input_filepath}',{offset}) Complete. Duration {int(end_time - start_time)} seconds")
        return output_filepath, ext
    except Exception as e:
        print("Exception:" + str(e))
        raise e

# Creates a low res mp4
def processVideo(input_filepath):
    try:
        start_time = perf_counter()

        nthreads = utils.getMaxThreads()

        print(f"processVideo('{input_filepath}') using {nthreads} threads")
        output_filepath = utils.getTmpFile()
        ext = '.mp4'
        ff = FFmpeg(
            global_options= f"-hide_banner -loglevel error -nostats -threads {nthreads}",
            inputs={input_filepath: None},
            outputs={
                output_filepath: '-c:v libx264 -f mp4 -b:v 500K -s 768x432 -movflags faststart -ar 48000 -preset medium'}
        )
        ff.run()
        end_time = perf_counter()
        print(f"processVideo('{input_filepath}') Complete. Duration {int(end_time - start_time)} seconds")
        return output_filepath, ext
    except Exception as e:
        print("Exception:" + str(e))
        raise e