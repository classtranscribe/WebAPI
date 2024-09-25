import subprocess
import os
import json
import re

def transcribe_audio_with_whisper(audio_file_path):
    if not os.path.exists(audio_file_path):
        raise FileNotFoundError(f"Audio file {audio_file_path} does not exist.")
    
    command = [
        "whisper",
        audio_file_path,
        "--model", "base.en",
        "--output_format", "json"
    ]

    try:
        result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=True)

        print("Whisper Output:")
        print(result.stdout)

        formatted_data = {"en": []}
        
        segments = result.stdout.strip().split('\n\n')
        for segment in segments:
            match = re.search(r'\[(\d+:\d+\.\d+)\s+-->\s+(\d+:\d+\.\d+)\]\s+(.*)', segment)
            if match:
                start_time = match.group(1)
                end_time = match.group(2)
                text = match.group(3).strip()

                formatted_data["en"].append({
                    "starttime": start_time,
                    "endtime": end_time,
                    "caption": text
                })

        return formatted_data

    except subprocess.CalledProcessError as e:
        print(f"Error during transcription: {e.stderr}")
        return None
    
    except Exception as e:
        print(f"An unexpected error occurred: {e}")
        return None

if __name__ == "__main__":
    audio_file = "randomvoice_16kHz.wav"

    transcription = transcribe_audio_with_whisper(audio_file)

    if transcription:
        print(json.dumps(transcription, indent=4))
    else:
        print("Transcription failed.")