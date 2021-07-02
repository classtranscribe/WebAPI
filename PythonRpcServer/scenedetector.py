import os
import math
from time import perf_counter 
from cv2 import cv2
import json
import numpy as np
from skimage.metrics import structural_similarity as ssim
from datetime import datetime
import pytesseract

DATA_DIR = os.getcwd()


def find_scenes(video_path, min_scene_length=1, abs_min=0.87, abs_max=0.98, find_subscenes=True, max_subscenes_per_minute=12):
    """
    Detects scenes within a video. 
    
    Calculates the structual similarity index measure (SSIM) between each subsequent frame then uses
    the list of SSIMs to identify where scene changes are. 
    
    Parameters:
    video_path (string): Path of the video to be used.
    min_scene_length (int): Minimum scene length in seconds. Default 1s
    abs_min (float): Minimum SSIM value for non-scene changes, i.e. any frame with SSIM < abs_min 
        is defined as a scene change. Default 0.7
    abs_max (float): Maximum SSIM value for scene_changes, i.e. any frame with SSIM > abs_max
        is defined as NOT a scene change. Default 0.98
    find_subscenes (boolean): Find subscenes or not. Default True
    max_subscenes_per_minute (int): Maximum number of subscenes per minute within a scene. If number
        of subscenes found exceeds max_subscenes_per_minute, then none of those subscenes are returned.
        Rational is that too many detected subscenes is more likely a result of a video clip or other
        noisy media and not actual scene changes. 
    
    Returns:
    string: List of dictionaries dumped to a JSON string. Each dict corresponds to a scene/subscene,
        with the key/item pairs being starting timestamp (start), image file name (img_file), ending 
        timestamp (end), and boolean indicating if it's a scene or subscene (is_subscene).
    """
    # Extract frames s1,e1,s2,e2,....
    # e1 != s2 but s1 is roughly equal to m1
    # s1 (m1) e1 s2 (m2) e2
    start_time = perf_counter()
    print(f"find_scenes({video_path}) starting...")
    try:
        # Check if the video file exsited
        video_total_path = os.path.join(DATA_DIR, video_path)
        if os.path.exists(video_total_path):
            print(f"{video_path}: Found file!")
        else:
            print(f"{video_path}: File not found - no scene cuts found")
            return json.dumps([])

        file_name = video_path[video_path.rfind('/')+1 : video_path.find('.')]
        directory = os.path.join(DATA_DIR, file_name)
        if not os.path.exists(directory):
            os.mkdir(directory)
            
        # Get the video capture and number of frames and fps
        cap = cv2.VideoCapture(video_path)
        num_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        fps = float(cap.get(cv2.CAP_PROP_FPS))

        targetFPS = 2
        everyN = max(1, int(fps / targetFPS)) # Input FPS could be < targetFPS
        print(f"{video_path}: frames={num_frames}. fps={fps}. Sampling every {everyN} frame")

        num_samples = num_frames // everyN

        # Mininum number of frames per scene
        min_samples_between_cut = min_scene_length * targetFPS

        # Stores the last frame read
        last_frame = 0
        # List of similarities (SSIMs) between frames
        similarities = np.zeros(num_samples)
        timestamps = np.zeros(num_samples)
        

        # For this loop only we are not using real frame numbers; we are skipping frames to improve processing speed
       

        for i in range(0,num_samples):
            # Read the next frame, resizing and converting to grayscale
            cap.set(cv2.CAP_PROP_POS_FRAMES,i * everyN )  
            ret, frame = cap.read()

            # Save the time stamp of each frame 
            timestamps[i] = cap.get(cv2.CAP_PROP_POS_MSEC)/1000
            
            curr_frame = cv2.cvtColor(cv2.resize(frame, (320,240)), cv2.COLOR_BGR2GRAY)

            # Calculate the SSIM between the current frame and last frame

            if i >= 1:
                sim = ssim(last_frame, curr_frame)
                similarities[i] = sim
                
            # Save the current frame for the next iteration
            last_frame = curr_frame
            

        # Find cuts by finding where SSIM < abs_min
        samples_cut_candidates = np.argwhere(similarities < abs_min).flatten()    
        
        print(f"{video_path}: {len(samples_cut_candidates)} candidates identified")       
        if len(samples_cut_candidates) == 0:
            print(f"{video_path}:Returning early - no scene cuts found")
            return json.dumps([])

        # Get real scene cuts by filtering out those that happen within min_frames of the last cut

        # What would happen to the output using real data if 'samples_cut_candidates[i-1]' is replaced with 'sample_cuts[-1]' ?
        # i.e. check the duration of the current scene being constructed, rather than the duration between the current dissimilar samples

        sample_cuts = [samples_cut_candidates[0] ]
        for i in range(1, len(samples_cut_candidates)):
            if samples_cut_candidates[i]  >= samples_cut_candidates[i-1] + min_samples_between_cut:
                sample_cuts += [ samples_cut_candidates[i]  ]

        if num_samples >1:
            sample_cuts += [num_samples-1]

        # Now work in frames again. Make sure we are using regular ints (not numpy ints) other json serialization will fail
        frame_cuts = [int(s * everyN) for s in sample_cuts]

        img_file = 'frame'

        # Initialize list of scenes
        scenes = []

        # Iterate through the scene cuts
        for i in range(1, len(frame_cuts)):
            scenes += [{'frame_start': frame_cuts[i-1] , 
                'frame_end': frame_cuts[i], 
                'is_subscene': False,
                }]

        cut_detect_time = perf_counter()
        print(f"find_scenes('{video_path}',...) Scene Cut Phase Complete.  Time so far {int(cut_detect_time - start_time)} seconds. Starting Image extraction and OCR")

        # Write the image file for each scene and convert start/end to timestamp
        for i, scene in enumerate(scenes):
            requested_frame_number = (scene['frame_start'] + scene['frame_end']) // 2
            cap.set(cv2.CAP_PROP_POS_FRAMES, requested_frame_number)  
            res, frame = cap.read()
            
            img_file = os.path.join(DATA_DIR, file_name, file_name + "_frame-%d.jpg" % requested_frame_number)
            cv2.imwrite(img_file, frame)

            #str_text = pytesseract.image_to_string(frame)
            #phrases = [phrase for phrase in str_text.split('\n') if len(phrase) > 0]
            
            str_text = pytesseract.image_to_data(frame, output_type='dict')

            phrases = []
            last_block = -1
            phrase = []
            for i in range(len(str_text['conf'])):
                if int(str_text['conf'][i]) >= 80 and len(str_text['text'][i].strip()) > 0:
                    curr_block = str_text['block_num'][i]
                    if curr_block != last_block:
                        if len(phrase) > 0:
                            phrases.append(' '.join(phrase))       
                        last_block = curr_block
                        phrase = []
                    phrase.append(str_text['text'][i])
            if len(phrase) > 0:
                phrases.append(' '.join(phrase))   
            
            # we dont want microsecond accuracy; the [:12] cuts off the last 3 unwanted digits
            scene['start'] = datetime.utcfromtimestamp(timestamps[scene['frame_start']// everyN]).strftime("%H:%M:%S.%f")[:12]
            scene['end'] = datetime.utcfromtimestamp(timestamps[scene['frame_end'] // everyN ]).strftime("%H:%M:%S.%f")[:12]            
            scene['img_file'] = img_file
            scene['raw_text'] = str_text # Internal debug format; subject to change uses phrases instead
            scene['phrases'] = phrases # list of strings
        
        end_time = perf_counter()
        print(f"find_scenes('{video_path}',...) Complete. Total Duration {int(end_time - start_time)} seconds")
        return json.dumps(scenes)
    
    except Exception as e:
        print(f"findScene(video_path) throwing Exception:" + str(e))
        raise e

