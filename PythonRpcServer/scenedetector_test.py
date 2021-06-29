import scenedetector as sd
import time
import json
import os
import urllib.request
import shutil

DATA_DIR = os.getcwd()

# General Testing Scheme For All Test Cases
def test_scheme(folder_name, url, expected_phrases):
    print("----------" + folder_name + "---STARTED----------")

    video_name = folder_name + '.mp4'
    video_path = DATA_DIR + '/' + video_name
    folder_path = DATA_DIR + '/' + folder_name

    # Download the video file
    urllib.request.urlretrieve(url, video_name) 

    # Run SceneDetector on the video 
    toy_lecture_json = sd.find_scenes(video_name)

    scenes = json.loads(toy_lecture_json)
    
    corpus = []
    for scene in scenes:
        corpus.append(scene['phrases'])
    
    raw_phrases = '\n'.join( ['\n'.join(words) for words in corpus] )
    raw_phrases = raw_phrases.replace('``','')

    # Check phrase occurance
    for phrase in expected_phrases:
        assert(phrase in raw_phrases)
        print("Phrase " + phrase + " was the OCR output")

    # Delete files
    os.remove(video_path)
    shutil.rmtree(folder_path)

    print("----------" + folder_name + "---Passed----------")

def run_scenedetector_tests():
    video_names = ['test_1_toy_lecture', 'test_2_241_thread']
    urls = [
        'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=pec6m3vbjzu2l4d2m1gv9588npq9nw7o&file_id=f_827175578540',
        'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=xbs7aqcsnrjdglplc6neh3fxdhkqz2fi&file_id=f_827616401395'
    ]
    expected_phrases_list = [
        ['Slide One', 'float', 'int'],
        ['pthread_create', 'Compile', 'stacks', 'printf', 'nothing happens']
    ]
    for i in range(len(video_names)):
        test_scheme(video_names[i], urls[i], expected_phrases_list[i])

if __name__ == '__main__': 
	run_scenedetector_tests();
	print('done');
