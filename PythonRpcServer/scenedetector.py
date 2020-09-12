import os
import cv2
import json
from time import perf_counter 



DATA_DIR = os.getenv('DATA_DIRECTORY')


# Standard PySceneDetect imports:
from scenedetect.video_manager import VideoManager
from scenedetect.scene_manager import SceneManager
# For caching detection metrics and saving/loading to a stats file
from scenedetect.stats_manager import StatsManager

# For content-aware scene detection:
from scenedetect.detectors.content_detector import ContentDetector
from scenedetect.detectors.threshold_detector import ThresholdDetector


# Some emperical evidence suggests that this job might be using more than one core

def find_scenes(video_path):

    try:
        start_time = perf_counter()
        print(f"find_scenes({video_path})")
        # type: (str) -> List[Tuple[FrameTimecode, FrameTimecode]]

        file_name =  video_path[video_path.rfind('/')+1 : video_path.find('.')]
        dir = os.path.join(DATA_DIR, file_name)
        if not os.path.exists(dir):
            os.mkdir(dir)


        #cap = cv2.VideoCapture(video_path)
        video_manager = VideoManager([video_path])
        stats_manager = StatsManager()
        # Construct our SceneManager and pass it our StatsManager.
        scene_manager = SceneManager(stats_manager)

        # Add ContentDetector algorithm (each detector's constructor
        # takes detector options, e.g. threshold).
        scene_manager.add_detector(ContentDetector(threshold=2, min_scene_len=100))
        #scene_manager.add_detector(ThresholdDetector(threshold=4))
        base_timecode = video_manager.get_base_timecode()

        # We save our stats file to {VIDEO_PATH}.stats.csv.
        stats_file_path = '%s.stats.csv' % video_path
        scene_list = []

        try:
            # If stats file exists, load it.
            if os.path.exists(stats_file_path):
                # Read stats from CSV file opened in read mode:
                with open(stats_file_path, 'r') as stats_file:
                    stats_manager.load_from_csv(stats_file, base_timecode)

            # Set downscale factor to improve processing speed.
            video_manager.set_downscale_factor()

            # Start video_manager.
            video_manager.start()

            # Perform scene detection on video_manager.
            scene_manager.detect_scenes(frame_source=video_manager)

            # Obtain list of detected scenes.
            scene_list = scene_manager.get_scene_list(base_timecode)
            
            # We only write to the stats file if a save is required:
            if stats_manager.is_save_required():
                with open(stats_file_path, 'w') as stats_file:
                    stats_manager.save_to_csv(stats_file, base_timecode)

        finally:
            video_manager.release()
        
        print(f"find_scenes({video_path}) - phase 2, Extract jpg")

        cap = cv2.VideoCapture(video_path)

        verbose = False
        if verbose : 
            print('List of scenes obtained:')
        # Each scene is a tuple of (start, end) FrameTimecodes.

        scenes = []
            
        for i, scene in enumerate(scene_list):
            if verbose:
                print('Scene %2d: Start %s / Frame %d, End %s / Frame %d' % (
                        i+1,
                        scene[0].get_timecode(), scene[0].get_frames(),
                        scene[1].get_timecode(), scene[1].get_frames(),))
            cap.set(cv2.CAP_PROP_POS_FRAMES, scene[0].get_frames() + (scene[1].get_frames() - scene[0].get_frames())//2)
            frame_no = scene[0].get_frames()
            if verbose:
                print('Frame no.', frame_no)
            res, frame = cap.read()
            img_file = os.path.join(DATA_DIR, file_name, "%d.jpg"%i)
            cv2.imwrite(img_file, frame)
            scenes.append({"start" : scene[0].get_timecode(), "img_file": img_file, "end": scene[1].get_timecode()})

        end_time = perf_counter()
        print(f"findScene() Complete. Returning {len(scenes)} scene(s). Duration {int(end_time - start_time)} seconds")

        return json.dumps(scenes)
    except Exception as e:
        print("findScene() throwing Exception:" + str(e))
        raise e
    