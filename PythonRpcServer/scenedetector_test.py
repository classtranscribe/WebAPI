import scenedetector as sd
import time
import os
import json
import pytube

DATA_DIR = os.getcwd()

def test_simple_lecture_video():
    # toy_lecture.mp4
    print("----------test_simple_lecture_video---STARTED----------")

    # Download the video file
    toy_lecture_url = 'https://www.youtube.com/watch?v=nF8c9kAtGVU'
    toy_lecture_file = pytube.YouTube(toy_lecture_url)
    toy_lecture_video = toy_lecture_file.streams.first()
    toy_lecture_video.download()

    video_name = toy_lecture_video.title + '.mp4'
    video_path = DATA_DIR + '/' + toy_lecture_video.title + '.mp4'

    # Run SceneDetector on the video 
    toy_lecture_json = sd.find_scenes(video_name)

    scenes = json.loads(toy_lecture_json)
    
    corpus = []
    for scene in scenes:
        corpus.append(scene['phrases'])
    
    raw_phrases = '\n'.join( ['\n'.join(words) for words in corpus] )
    raw_phrases = raw_phrases.replace('``','')
    print('raw_phrases', raw_phrases)

    # Check phrase occurance
    expected_phrases = ['Slide One', 'Slide Two', 'Slide Three']
    for phrase in expected_phrases:
        assert(phrase in raw_phrases)

    # Delete the file
    os.remove(video_path)

    print("----------test_simple_lecture_video---PASSED----------")

def run_scenedetector_tests():
	test_simple_lecture_video()

if __name__ == '__main__': 
	run_scenedetector_tests();
	print('done');
