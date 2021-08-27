import os
import math
import json
import numpy as np
import pytesseract
import titledetector as td
import cv2
import decord
from time import perf_counter
from skimage.metrics import structural_similarity as ssim
from datetime import datetime
from collections import Counter
from mtcnn_cv2 import MTCNN

DATA_DIR = os.getenv('DATA_DIRECTORY')
TARGET_FPS = float(os.getenv('SCENE_DETECT_FPS', 0.5))
SCENE_DETECT_USE_FACE = os.getenv('SCENE_DETECT_USE_FACE', 'true') == 'true'
SCENE_DETECT_USE_OCR = os.getenv('SCENE_DETECT_USE_OCR', 'true') == 'true'

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
    gray_frame = cv2.cvtColor(cv2.resize(
        curr_frame, (320, 240)), cv2.COLOR_BGR2RGB)

    # Run the face detection
    faces = detector.detect_faces(gray_frame)

    curr_frame_boxes = []  # [x1, x2, y1, y2]
    has_body = False

    # Iterate through all the bounding boxes for one frame
    for face in faces:
        x, y, width, height = face['box']
        curr_frame_boxes.append([x, x + width, y, y + height])

        # Move x to the center of the face bounding box
        x = x + width / 2

        # Check if the face is at the center
        if x > 0.2 * gray_frame.shape[1] and x < 0.8 * gray_frame.shape[1]:

            # Check if the face is large enough
            if width / gray_frame.shape[1] > 0.1 or height / gray_frame.shape[0] > 0.1:
                has_body = True
                body_x = int(x - 2 * width)
                if body_x < 0:
                    body_x = 0

                body_y = y + height
                body_width = width * 4
                body_height = height * 3

                curr_frame_boxes.append(
                    [body_x, body_x + body_width, body_y, body_y + body_height])

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

def generate_frame_similarity(video_path, num_samples, everyN, start_time):
    """
    Generate simlarity values for each sample frames.

    Parameters:
    video_path (string): Video path
    num_samples (list of float): Amount of samples
    everyN (list of float): Number of frames that is ignored each iteration
    start_time (list of float): Start time of the whole process

    Returns:
    List of string: Timestamps array of each sample frame
    List of float: sim_structural array of each sample frame
    List of float: sim_structural_no_face array of each sample frame
    List of float: sim_ocr array of each sample frame
    """

    SIM_OCR_CONFIDENCE = 55  # OCR confidnece used to generate sim_ocr

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

    # Video Reader
    vr_full = decord.VideoReader(video_path, ctx=decord.cpu(0))
    last_log_time = 0
    # For this loop only we are not using real frame numbers; we are skipping frames to improve processing speed
    for i in range(0, num_samples):

        t = perf_counter()
        if t >= last_log_time + 30:
            print(
                f"find_scenes({video_path}): {i}/{num_samples}. Elapsed {int(t-start_time)} s")
            last_log_time = t
        
        # Read the next frame, resizing and converting to grayscale
        frame_vr = vr_full[i * everyN]
        frame = cv2.cvtColor(frame_vr.asnumpy(), cv2.COLOR_RGB2BGR)

        # Save the time stamp of each frame
        timestamps[i] = vr_full.get_frame_timestamp(i * everyN)[0]

        curr_frame = cv2.cvtColor(cv2.resize(
            frame, (320, 240)), cv2.COLOR_BGR2GRAY)

        # Calculate the SSIM between the current frame and last frame
        if i >= 1:
            sim_structural[i] = ssim(last_frame, curr_frame)

        if SCENE_DETECT_USE_FACE:
            # Run Face Detection upon the current frame
            curr_face_detection_result = require_face_result(curr_frame)

            # Calculate the SSIM between the current frame and last frame when face & upper body are removed
            if i >= 1:
                sim_structural_no_face[i] = require_ssim_with_face_detection(
                    curr_frame, curr_face_detection_result, last_frame, last_face_detection_result)

            # Save the current face detection result for the next iteration
            last_face_detection_result = curr_face_detection_result
        else:
            sim_structural_no_face[i] = sim_structural[i]

        if SCENE_DETECT_USE_OCR:
            # Calculate the OCR difference between the current frame and last frame
            ocr_frame = cv2.cvtColor(cv2.resize(
                frame, (480, 360)), cv2.COLOR_BGR2GRAY)
            str_text = pytesseract.image_to_data(
                ocr_frame, output_type='dict')

            phrases = Counter()
            for j in range(len(str_text['conf'])):
                if int(str_text['conf'][j]) >= SIM_OCR_CONFIDENCE and len(str_text['text'][j].strip()) > 0:
                    phrases[str_text['text'][j]
                            ] += (float(str_text['conf'][j]) / 100)

            curr_ocr = dict(phrases)

            if i >= 1:
                sim_ocr[i] = compare_ocr_difference(last_ocr, curr_ocr)

            ocr_output.append(phrases)

            # Save the current OCR output for the next iteration
            last_ocr = curr_ocr
        else:
            sim_ocr[i] = 1 if i >= 1 else 0

        # Save the current frame for the next iteration
        last_frame = curr_frame
    
        # Delete local variables
        del frame_vr
        del frame
        del curr_frame

        del curr_face_detection_result
        del str_text
        del phrases
        del curr_ocr
    
    return timestamps, sim_structural, sim_structural_no_face, sim_ocr

def extract_scene_information(video_path, timestamps, frame_cuts, everyN, start_time):
    """
    Extract useful features from each detected scenes and output scene images.

    Parameters:
    video_path (string): Video path
    timestamps (list of float): Timestamp array for sample frames
    frame_cuts (list of float): Frame number array for sample frames
    everyN (list of float): Number of frames that is ignored
    start_time (list of float): Start time of the whole process

    Returns:
    string: Features of detected scene as JSOH
    """

    OCR_CONFIDENCE = 80  # OCR confidnece used to extract text in detected scenes. Higher confidence to extract insightful information

    # we don't want the '.mp4' extension (if it exists)
    short_file_name = video_path[
        video_path.rfind('/') + 1: video_path.find('.')]

    out_directory = os.path.join(DATA_DIR, 'frames', short_file_name)

    # Initialize list of scenes
    scenes = []

    # Iterate through the scene cuts
    for i in range(1, len(frame_cuts)):
        scenes += [{'frame_start': frame_cuts[i - 1],
                    'frame_end': frame_cuts[i]}]

    cut_detect_time = perf_counter()
    print(
        f"find_scenes('{video_path}',...) Scene Cut Phase Complete.  Time so far {int(cut_detect_time - start_time)} seconds. Starting Image extraction and OCR")

    # Write the image file for each scene and convert start/end to timestamp

    os.makedirs(out_directory, exist_ok=True)
    last_log_time = 0

    # Video Reader
    vr_full = decord.VideoReader(video_path, ctx=decord.cpu(0))

    for i, scene in enumerate(scenes):
        requested_frame_number = (
            scene['frame_start'] + scene['frame_end']) // 2

        t = perf_counter()
        if t >= last_log_time + 30:
            print(
                f"find_scenes({video_path}): {i}/{len(scenes)}. Elapsed {int(t-cut_detect_time)} s")
            last_log_time = t

        # Read a frame through decord
        frame_vr = vr_full[requested_frame_number]

        frame = cv2.cvtColor(frame_vr.asnumpy(), cv2.COLOR_RGB2BGR)

        img_file = os.path.join(
            out_directory, f"{short_file_name}_frame-{requested_frame_number}.jpg")
        cv2.imwrite(img_file, frame)

        # OCR generation
        gray_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        str_text = pytesseract.image_to_data(
            gray_frame, output_type='dict')

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
        # Internal debug format; subject to change uses phrases instead
        scene['raw_text'] = str_text
        scene['phrases'] = phrases  # list of strings
        scene['title'] = title  # detected title as string

    return scenes

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
    MIN_SCENE_LENGTH = 1  # Minimum scene length in seconds

    assert (os.path.exists(DATA_DIR))

    # Extract frames s1,e1,s2,e2,....
    # e1 != s2 but s1 is roughly equal to m1
    # s1 (m1) e1 s2 (m2) e2

    start_time = perf_counter()
    print(f"find_scenes({video_path}) starting...")
    print(
        f"SCENE_DETECT_USE_FACE={SCENE_DETECT_USE_FACE}, SCENE_DETECT_USE_OCR={SCENE_DETECT_USE_OCR}, TARGET_FPS={TARGET_FPS}")
    try:
        # Check if the video file exsited

        if os.path.exists(video_path):
            print(f"{video_path}: Found file!")
        else:
            print(f"{video_path}: File not found -returning empty scene cuts ")
            return json.dumps([])

        # we don't want the '.mp4' extension (if it exists)
        short_file_name = video_path[
            video_path.rfind('/') + 1: video_path.find('.')]

        out_directory = os.path.join(DATA_DIR, 'frames', short_file_name)

        # Get the video capture and number of frames and fps
        cap = cv2.VideoCapture(video_path)
        num_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        fps = float(cap.get(cv2.CAP_PROP_FPS))

        # Input FPS could be < targetFPS
        everyN = max(1, int(fps / TARGET_FPS))
        print(
            f"find_scenes({video_path}): frames={num_frames}. fps={fps}. Sampling every {everyN} frame")

        num_samples = num_frames // everyN

        # Mininum number of frames per scene
        min_samples_between_cut = max(0, int(MIN_SCENE_LENGTH * TARGET_FPS))

        # Scene Analysis
        timestamps, sim_structural, sim_structural_no_face, sim_ocr = generate_frame_similarity(video_path, num_samples, everyN, start_time)

        t = perf_counter()
        print(
            f"find_scenes('{video_path}',...) Scene Analysis Complete.  Time so far {int(t - start_time)} seconds. Defining Scene Cut points next")

        # Calculate the combined similarities score
        combined_similarities = calculate_score(
            sim_structural, sim_ocr, sim_structural_no_face)

        # Find cuts by finding where combined similarities < ABS_MIN
        samples_cut_candidates = np.argwhere(
            combined_similarities < ABS_MIN).flatten()

        print(f"{video_path}: {len(samples_cut_candidates)} candidates identified")
        if len(samples_cut_candidates) == 0:
            print(f"{video_path}:Returning early - no scene cuts found")
            return json.dumps([])

        # Get real scene cuts by filtering out those that happen within min_frames of the last cut
        sample_cuts = [samples_cut_candidates[0]]
        for i in range(1, len(samples_cut_candidates)):
            if samples_cut_candidates[i] >= samples_cut_candidates[i - 1] + min_samples_between_cut:
                sample_cuts += [samples_cut_candidates[i]]

        if num_samples > 1:
            sample_cuts += [num_samples - 1]

        # Now work in frames again. Make sure we are using regular ints (not numpy ints) other json serialization will fail
        frame_cuts = [int(s * everyN) for s in sample_cuts]

        # Image Extraction and OCR
        scenes = extract_scene_information(video_path, timestamps, frame_cuts, everyN, start_time)

        return json.dumps(scenes)

    except Exception as e:
        print(f"find_scenes({video_path}) throwing Exception:" + str(e))
        raise e