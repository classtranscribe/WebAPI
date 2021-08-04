import os
import math
import json
import numpy as np
import pytesseract
import titledetector as td
from time import perf_counter
from cv2 import cv2
from skimage.metrics import structural_similarity as ssim
from datetime import datetime
from collections import Counter
from mtcnn import MTCNN

DATA_DIR = os.getenv('DATA_DIRECTORY')
detector = MTCNN()

def require_face_result(curr_frame):
    """
    Find all the bounding boxes of face & upper body appeared in a given frame.

    Parameters:
    curr_frame (image): Frame image

    Returns:
    tuple: 
        First element: a boolean indicating if there is any face & upper body found inside the frame
        Second element: a list of bounding boxes of face & upper body
    """

    # Convert the input image to gray scale    
    gray_frame = cv2.cvtColor(cv2.resize(curr_frame, (320, 240)), cv2.COLOR_BGR2RGB)

    # Run the face detection
    faces = detector.detect_faces(gray_frame)
    
    curr_frame_boxes = []  # [x1, x2, y1, y2]
    has_body = False

    # Iterate through all the bounding boxes for one frame
    for face in faces:
        bounding_box = face['box']
        x, y, width, height = bounding_box
        curr_frame_boxes.append([x, x + width, y, y + height])

        # Move x to the center of the face bounding box
        x = x + width / 2

        # Check if the face is at the center
        if x > 0.2 * curr_frame.shape[1] and x < 0.8 * curr_frame.shape[1]:

            # Check if the face is large enough
            if width / curr_frame.shape[1] > 0.1 or height / curr_frame.shape[0] > 0.1:
                has_body = True
                body_x = int(x - 2 * width)
                if body_x < 0:
                    body_x = 0

                body_y = y + height
                body_width = width * 4
                body_height = height * 3

                curr_frame_boxes.append([body_x, body_x + body_width, body_y, body_y + body_height])
    
    return (has_body, curr_frame_boxes)

def require_ssim_with_face_detection(curr_frame, curr_result, last_frame, last_result):
    """
    Given two frames with their face & upper body bounding boxes, 
        find SSIM between them after removing face & upper body

    Parameters:
    curr_frame (image): Image of the first frame
    curr_result (tuple): Face & upper body detection result of the first frame
    last_frame (image): Image of the second frame
    last_result (tuple): Face & upper body detection result of the second frame

    Returns:
    float: SSIM after removing face & upper body
    """

    curr_frame_with_face_removed = curr_frame.copy()
    last_frame_with_face_removed = last_frame.copy()

    if curr_result[0]:
        curr_boxes = curr_result[1]
        for j in range(len(curr_boxes)):
            x1, x2, y1, y2 = curr_boxes[j]
            curr_frame_with_face_removed[x1:x2, y1:y2] = 0
            last_frame_with_face_removed[x1:x2, y1:y2] = 0

    if last_result[0]:
        last_boxes = last_result[1]
        for j in range(len(last_boxes)):
            x1, x2, y1, y2 = last_boxes[j]
            curr_frame_with_face_removed[x1:x2, y1:y2] = 0
            last_frame_with_face_removed[x1:x2, y1:y2] = 0
    
    return ssim(last_frame_with_face_removed, curr_frame_with_face_removed)



def compare_ocr_difference(word_dict_a, word_dict_b):
    """
    Calculate the sim_OCR between two frames.

    Parameters:
    word_dict_a (dict): Key is the words that appeared in the OCR output for frame A
                        Value is the sum of confidence of each word
    word_dict_b (dict): Key is the words that appeared in the OCR output for frame B
                        Value is the sum of confidence of each word

    Returns:
    float: Relative OCR similarty between the two frames
    """

    total_amount = 0
    for k in word_dict_a.keys():
        total_amount += word_dict_a[k]
    for k in word_dict_b.keys():
        total_amount += word_dict_b[k]

    if total_amount == 0:
        return 1.0

    score = 0
    for key_a in word_dict_a.keys():
        if key_a in word_dict_b.keys():
            score += (word_dict_a[key_a] + word_dict_b[key_a])

    for key_b in list(set(word_dict_b.keys()) - set(word_dict_a.keys())):
        if key_b in word_dict_a.keys():
            score += (word_dict_a[key_b] + word_dict_b[key_b])

    return score / total_amount


def calculate_score(sim_structural, sim_ocr, sim_structural_no_face):
    """
    Calculate the final similarties score between two frames.

    Parameters:
    sim_structural (list of float): List of similarities (SSIMs) between frames
    sim_ocr (list of float): List of OCR similarities
    sim_structural_no_face (list of float): List of similarities (SSIMs) between frames when face is removed

    Returns:
    list of float: List of combined_similarities between frames
    """
    return 0.3 * sim_structural + 0.3 * sim_structural_no_face + 0.4 * sim_ocr


def find_scenes(video_path):
    """
    Detects scenes within a video.

    Calculates the similarity between each subsequent frame to identify where scene changes are. Report key features
        about each scene change.

    Parameters:
    video_path (string): Path of the video to be used.

    Returns:
    string: List of dictionaries dumped to a JSON string. Each dict corresponds to a scene/subscene,
        with the key/item pairs being:
        frame_start (int): Numbering of the frame where the scene starts
        frame_end (int): Numbering of the frame where the scene ends
        is_subscene (boolean): Indicating if it's a scene or subscene
        start (string): Starting timestamp of the scene
        end (string): Ending timestamp of the scene
        img_file (string): File name of the detected scene image
        raw_text (dict): Raw pytesseract result as a dict
        phrases (list of strings): Cleaned pytesseract result as a list of strings
        title (string): Detected title of the current scene
    """

    # CONSTANTS
    ABS_MIN = 0.7  # Minimum combined_similarities value for non-scene changes, i.e. any frame with combined_similarities < ABS_MIN is defined as a scene change
    OCR_CONFIDENCE = 80  # OCR confidnece used to extract text in detected scenes. Higher confidence to extract insightful information
    SIM_OCR_CONFIDENCE = 55  # OCR confidnece used to generate sim_ocr
    MIN_SCENE_LENGTH = 1  # Minimum scene length in seconds

    assert (os.path.exists(DATA_DIR))

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
            print(f"{video_path}: File not found -returning empty scene cuts ")
            return json.dumps([])

        short_file_name = video_path[
                          video_path.rfind('/') + 1: video_path.find('.')]  # short filename without extension
        directory = os.path.join(DATA_DIR, short_file_name)

        # Get the video capture and number of frames and fps
        cap = cv2.VideoCapture(video_path)
        num_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        fps = float(cap.get(cv2.CAP_PROP_FPS))

        targetFPS = 2
        everyN = max(1, int(fps / targetFPS))  # Input FPS could be < targetFPS
        print(f"{video_path}: frames={num_frames}. fps={fps}. Sampling every {everyN} frame")

        num_samples = num_frames // everyN

        # Mininum number of frames per scene
        min_samples_between_cut = MIN_SCENE_LENGTH * targetFPS


        # Stores the last frame read
        last_frame = 0

        # Stores the last face detetion result
        last_face_detection_result = 0

        # Stores the OCR output of last frame read
        last_ocr = dict()

        # List of similarities (SSIMs) between frames
        sim_structural = np.zeros(num_samples)

        # List of OCR outputs and OCR similarities
        ocr_output = []
        sim_ocr = np.zeros(num_samples)

        # List of similarities (SSIMs) between frames when face is removed
        sim_structural_no_face = np.zeros(num_samples)

        timestamps = np.zeros(num_samples)

        # For this loop only we are not using real frame numbers; we are skipping frames to improve processing speed

        for i in range(0, num_samples):
            # Read the next frame, resizing and converting to grayscale
            cap.set(cv2.CAP_PROP_POS_FRAMES, i * everyN)
            ret, frame = cap.read()

            # Save the time stamp of each frame
            timestamps[i] = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000

            curr_frame = cv2.cvtColor(cv2.resize(frame, (320, 240)), cv2.COLOR_BGR2GRAY)

            # Calculate the SSIM between the current frame and last frame
            if i >= 1:
                sim_structural[i] = ssim(last_frame, curr_frame)

            # Run Face Detection upon the current frame
            curr_face_detection_result = require_face_result(curr_frame)

            # Calculate the SSIM between the current frame and last frame when face & upper body are removed
            if i >= 1:
                sim_structural_no_face[i] = require_ssim_with_face_detection(curr_frame, curr_face_detection_result, last_frame, last_face_detection_result)
            
            # Calculate the OCR difference between the current frame and last frame
            ocr_frame = cv2.cvtColor(cv2.resize(frame, (480, 360)), cv2.COLOR_BGR2GRAY)
            str_text = pytesseract.image_to_data(ocr_frame, output_type='dict')

            phrases = Counter()
            for j in range(len(str_text['conf'])):
                if int(str_text['conf'][j]) >= SIM_OCR_CONFIDENCE and len(str_text['text'][j].strip()) > 0:
                    phrases[str_text['text'][j]] += (float(str_text['conf'][j]) / 100)

            curr_ocr = dict(phrases)

            if i >= 1:
                sim_ocr[i] = compare_ocr_difference(last_ocr, curr_ocr)

            ocr_output.append(phrases)

            # Save the current frame for the next iteration
            last_frame = curr_frame

            # Save the current face detection result for the next iteration
            last_face_detection_result = curr_face_detection_result

            # Save the current OCR output for the next iteration
            last_ocr = curr_ocr

        #for i in range(len(sim_structural)):
        #    print(i, round(sim_structural[i], 3), round(sim_structural_no_face[i], 3), round(sim_ocr[i], 3))

        # Calculate the combined similarities score
        combined_similarities = calculate_score(sim_structural, sim_ocr, sim_structural_no_face)

        # Find cuts by finding where combined similarities < ABS_MIN
        samples_cut_candidates = np.argwhere(combined_similarities < ABS_MIN).flatten()

        print(f"{video_path}: {len(samples_cut_candidates)} candidates identified")
        if len(samples_cut_candidates) == 0:
            print(f"{video_path}:Returning early - no scene cuts found")
            return json.dumps([])

        # Get real scene cuts by filtering out those that happen within min_frames of the last cut

        # What would happen to the output using real data if 'samples_cut_candidates[i-1]' is replaced with 'sample_cuts[-1]' ?
        # i.e. check the duration of the current scene being constructed, rather than the duration between the current dissimilar samples

        sample_cuts = [samples_cut_candidates[0]]
        for i in range(1, len(samples_cut_candidates)):
            if samples_cut_candidates[i] >= samples_cut_candidates[i - 1] + min_samples_between_cut:
                sample_cuts += [samples_cut_candidates[i]]

        if num_samples > 1:
            sample_cuts += [num_samples - 1]

        # Now work in frames again. Make sure we are using regular ints (not numpy ints) other json serialization will fail
        frame_cuts = [int(s * everyN) for s in sample_cuts]

        # Initialize list of scenes
        scenes = []

        # Iterate through the scene cuts
        for i in range(1, len(frame_cuts)):
            scenes += [{'frame_start': frame_cuts[i - 1], 'frame_end': frame_cuts[i]}]

        cut_detect_time = perf_counter()
        print(
            f"find_scenes('{video_path}',...) Scene Cut Phase Complete.  Time so far {int(cut_detect_time - start_time)} seconds. Starting Image extraction and OCR")

        # Write the image file for each scene and convert start/end to timestamp

        os.makedirs(directory, exist_ok=True)

        for i, scene in enumerate(scenes):
            requested_frame_number = (scene['frame_start'] + scene['frame_end']) // 2
            cap.set(cv2.CAP_PROP_POS_FRAMES, requested_frame_number)
            res, frame = cap.read()

            img_file = os.path.join(directory, f"{short_file_name}_frame-{requested_frame_number}.jpg")
            cv2.imwrite(img_file, frame)

            # OCR generation
            gray_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            str_text = pytesseract.image_to_data(gray_frame, output_type='dict')

            phrases = []
            last_block = -1
            phrase = []
            for i in range(len(str_text['conf'])):
                if int(str_text['conf'][i]) >= OCR_CONFIDENCE and len(str_text['text'][i].strip()) > 0:
                    curr_block = str_text['block_num'][i]
                    if curr_block != last_block:
                        if len(phrase) > 0:
                            phrases.append(' '.join(phrase))
                        last_block = curr_block
                        phrase = []
                    phrase.append(str_text['text'][i])
            if len(phrase) > 0:
                phrases.append(' '.join(phrase))

                # Title generation
            frame_height, frame_width, frame_channels = frame.shape
            title = td.title_detection(str_text, frame_height, frame_width)

            # we dont want microsecond accuracy; the [:12] cuts off the last 3 unwanted digits
            scene['start'] = datetime.utcfromtimestamp(timestamps[scene['frame_start'] // everyN]).strftime(
                "%H:%M:%S.%f")[:12]
            scene['end'] = datetime.utcfromtimestamp(timestamps[scene['frame_end'] // everyN]).strftime("%H:%M:%S.%f")[
                           :12]
            scene['img_file'] = img_file
            scene['raw_text'] = str_text  # Internal debug format; subject to change uses phrases instead
            scene['phrases'] = phrases  # list of strings
            scene['title'] = title  # detected title as string

        end_time = perf_counter()
        print(f"find_scenes('{video_path}',...) Complete. Total Duration {int(end_time - start_time)} seconds")
        return json.dumps(scenes)

    except Exception as e:
        print(f"findScene(video_path) throwing Exception:" + str(e))
        raise e


