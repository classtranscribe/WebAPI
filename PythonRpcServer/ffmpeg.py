from ffmpy import FFmpeg
from utils import getTmpFile

def convertVideoToWav(input_filepath):
    output_filepath = getTmpFile()
    ext = '.wav'
    ff = FFmpeg(
    inputs={input_filepath: None},
    outputs={output_filepath: '-c:a pcm_s16le -ac 1 -y -ar 16000 -f wav'}
    )
    ff.run()
    return output_filepath, ext

def processVideo(input_filepath):
    output_filepath = getTmpFile()
    ext = '.mp4'
    ff = FFmpeg(
    inputs={input_filepath: None},
    outputs={output_filepath: '-c:v libx264 -f mp4 -b:v 500K -s 768x432 -movflags faststart -ar 48000 -preset medium'}
    )
    ff.run()
    return output_filepath, ext