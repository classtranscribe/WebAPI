import scenedetector as sd
import time
import json
import os
import urllib.request
import shutil
import cv2
from skimage.metrics import structural_similarity as ssim

DATA_DIR = os.getcwd()

# General Testing Scheme For All Test Cases
def test_scheme(folder_name, url, expected_phrases):
    # General Testing Scheme
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
        print("Phrase " + phrase + " was in the OCR output")
    
    # Check frame number
    frame_list = os.listdir(folder_path)
    for frame_name in frame_list:
        frame_path = folder_path + '/' + frame_name
        output_frame = cv2.imread(frame_path)
        frame_number = frame_name[frame_name.rfind('-')+1 : frame_name.find('.')]
        
        cap = cv2.VideoCapture(video_path)
        cap.set(cv2.CAP_PROP_POS_FRAMES, int(frame_number) )  
        ret, frame = cap.read()
        
        rezied_frame = cv2.cvtColor(cv2.resize(frame, (320,240)), cv2.COLOR_BGR2GRAY)
        resized_output_frame = cv2.cvtColor(cv2.resize(output_frame, (320,240)), cv2.COLOR_BGR2GRAY)
        
        sim = ssim(rezied_frame, resized_output_frame)
        print('Frame ' + frame_number + " Simlarity: " + str(sim))
        assert(sim > 0.99)

    # Delete files
    os.remove(video_path)
    shutil.rmtree(folder_path)

    print("----------" + folder_name + "---Passed----------")

def run_scenedetector_tests():
    video_names = ['test_1_toy_lecture', 'test_2_241_thread', 'test_3_adv582_w3']
    urls = [
        'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=pec6m3vbjzu2l4d2m1gv9588npq9nw7o&file_id=f_827175578540',
        'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=xbs7aqcsnrjdglplc6neh3fxdhkqz2fi&file_id=f_827616401395',
        'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=t8lnoxyrmiff0g2xtvg9t5tlak0npnyl&file_id=f_828444986436'
    ]
    expected_phrases_list = [
        ['Slide One', 'float', 'int'],
        ['pthread_create', 'Compile', 'stacks', 'printf', 'nothing happens'],
        ['Merchants of Cool', 'Barak Goodman', 'New York Times']
    ]
    for i in range(len(video_names)):
        test_scheme(video_names[i], urls[i], expected_phrases_list[i])


if __name__ == '__main__': 
    if 'DATA_DIRECTORY' not in os.environ.keys():
        os.environ['DATA_DIRECTORY'] = str(os.getcwd())	
    run_scenedetector_tests()
    print('done')
