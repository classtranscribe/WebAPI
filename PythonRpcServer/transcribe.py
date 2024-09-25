import os
import subprocess
import json

# Path to the Whisper executable inside the container
WHISPER_EXECUTABLE = os.environ.get('WHISPER_EXE','whisper')  # Executable 'main' is assumed to be in the same directory as this script
MODEL = os.environ.get('WHISPER_MODEL','models/ggml-base.en.bin')

def transcribe_audio(media_filepath):

    if media_filepath == 'TEST-transcribe_example_result':
        result_json_file = 'transcribe_example_result.json'
        with open(result_json_file, 'r') as json_file:
            transcription_result = json.load(json_file)
        return transcription_result

    # Ensure the media file exists
    if not os.path.exists(media_filepath):
        raise FileNotFoundError(f"Media file not found: {media_filepath}")

    # Path to the output JSON file that Whisper will generate
    json_output_path = f"{media_filepath}.json"
    if os.path.exists(media_filepath):
        os.remove(json_output_path)
     
    # Command to run Whisper.cpp inside the container using the main executable
    whisper_command = [
        WHISPER_EXECUTABLE,                  # Path to Whisper executable
        '-ojf',                              # Output as JSON file
        '-f', media_filepath,                 # Media file path
        '-m', MODEL
    ]

    print("Running Whisper transcription inside the container...")
    
    # Execute the Whisper command
    result = subprocess.run(whisper_command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    # Handle command failure
    if result.returncode != 0:
        raise Exception(f"Whisper failed with error:\n{result.stderr.decode('utf-8')}")

    # Check if the output JSON file was generated
    if not os.path.exists(json_output_path):
        raise FileNotFoundError(f"Expected JSON output file not found: {json_output_path}")

    # Load the JSON transcription result
    with open(json_output_path, 'r') as json_file:
        transcription_result = json.load(json_file)
    
    # Delete the JSON file after reading it
    os.remove(json_output_path)
    print(f"Deleted the JSON file: {json_output_path}")

    return transcription_result

# Example usage
if __name__ == '__main__':
    # Example media file path inside the container (the actual path will depend on where the file is located)
    import sys
    if len(sys.argv) > 1:
        audio_filepath = sys.argv[1]
    else:
        audio_filepath = 'sharedVolume/recording0.wav'  # Update this path as needed
    
    try:
        transcription_result = transcribe_audio(audio_filepath)
        print("Transcription Result:", json.dumps(transcription_result, indent=4))
    except Exception as e:
        print(f"Error: {str(e)}")
