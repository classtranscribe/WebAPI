from ffmpy import FFmpeg
import subprocess
from time import perf_counter 
import utils
import json

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

def getMediaInfo(input_filepath):
    #Exception printing and timing is now handled by caller -see LogWorker
        # In seconds
        #https://gist.github.com/nrk/2286511
        staticargs = "-hide_banner -loglevel fatal -show_error -show_format -show_streams -show_programs -show_chapters -show_private_data -print_format json"
        jsonresult = subprocess.check_output(
            ['ffprobe','-i', input_filepath] + staticargs.split(' '),
            encoding='utf-8'
        )
        print(f'{input_filepath}: {jsonresult}')

        return json.dumps(jsonresult)

