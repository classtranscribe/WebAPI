# from pytube.extract import playlist_id

# from yt_dlp import YoutubeDL
import yt_dlp

import requests
from utils import getRandomString
import os
import json
from time import perf_counter 
import datetime

#from pytube import YouTube
# import pytube

from mediaprovider import MediaProvider, InvalidPlaylistInfoException

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY')
assert( DATA_DIRECTORY )

#YOUTUBE_API_KEY = os.getenv('YOUTUBE_API_KEY')
#YOUTUBE_PLAYLIST_URL = 'https://www.googleapis.com/youtube/v3/playlistItems'
#YOUTUBE_CHANNELS_URL = 'https://www.googleapis.com/youtube/v3/channels'
YOUTUBE_PLAYLIST_BASE_URL='https://www.youtube.com/playlist?list='
YOUTUBE_CHANNEL_BASE_URL='https://www.youtube.com/channel/'

class YoutubeProvider(MediaProvider):

    def getPlaylistItems(self, request):
        print(f'getPlaylistItems({request})')
        isChannel = False
        
        try:
            meta = json.loads(request.metadata.json)
            isChannel = meta['isChannel'] == '1'
        except:
            pass # Missing key, json is null etc

        medias = self.get_youtube_channel(request.Url) if isChannel else self.get_youtube_playlist(request.Url)

        return json.dumps(medias)

    def getMedia(self, request):
        return self.download_youtube_video(request.videoUrl)

    def get_youtube_channel(self, identifier):
        print(f'get_youtube_channel({identifier})')

        url = YOUTUBE_CHANNEL_BASE_URL+ identifier
        # Use yt_dlp to create a channel,
        
        channel = yt_dlp.Youtube(url).get_channel()
        ## channel.playlist_id = channel.playlist_id.replace('UC', 'UU')

        playlist_id = channel.playlist_id
        #according to one StackOver and one test, channels-to-playlists can also be converted with string replace  UCXXXX to UUXXXX
        print(f"channel {identifier}-> playlist {playlist_id}")
        return self.get_youtube_playlist(playlist_id)

    def get_youtube_playlist(self, identifier):
        try:
            start_time = perf_counter()
            
            url= YOUTUBE_PLAYLIST_BASE_URL + identifier
            print(f"get_youtube_playlist(identifier): {url}")
            
            ydl_opts = {
                'quiet': True,
                'extract_flat': 'in_playlist',  # Ensure we are extracting playlist entries
                'force_generic_extractor': True,
            }
            medias = []
            # Current time in iso date time format
            now = datetime.datetime.now().isoformat()
            with yt_dlp.YoutubeDL(ydl_opts) as ydl:
                info_dict = ydl.extract_info(url, download=False)
                for entry in info_dict.get( 'entries', []):
                    print(entry)
                    published_at = entry.get('upload_date', now)
                    media = {
                        "channelId": entry['channel_id'],
                        "playlistId": identifier,
                        "title": entry['title'],
                        "description": entry['description'],
                        "publishedAt": published_at,
                        "videoUrl": "https://youtube.com/watch?v="+entry['id'],
                        "videoId": entry['id'],
                        "createdAt": published_at
                    }
                    medias.append(media)
            end_time = perf_counter()
            print(f'Youtube playlist {identifier}: Returning {len(medias)} items. Processing time {end_time - start_time :.2f} seconds')
            return medias
        except Exception as e:
            print(f"get_youtube_playlist({identifier}) Exception:" + str(e))
            raise e        

    def download_youtube_video(self, youtubeUrl):
        try:
            print(f"download_youtube_video({youtubeUrl}): Starting")
            start_time = perf_counter()
            extension = '.mp4'
            filename = getRandomString(8)
            filepath =f'{DATA_DIRECTORY}/{filename}'
            ydl_opts = {
                'quiet': True,
                'format': 'best[ext=mp4]',
                'outtmpl': filepath,
                'cachedir' : False,
                'progress_hooks': [],
                'call_home': False,
                'no_color': True,
                'noprogress': True,
            }
            with yt_dlp.YoutubeDL(ydl_opts) as ydl:
                x = ydl.download([youtubeUrl])
                print(x)
                #filepath = yt_dlp.YoutubeDL(ydl_opts).streams.filter(subtype='mp4').get_highest_resolution().download(output_path = DATA_DIRECTORY, filename = filename)
            end_time = perf_counter()
            print(f"download_youtube_video({youtubeUrl}): Done. Downloaded in {end_time - start_time :.2f} seconds")
            return filepath, extension
        except Exception as e:
            print(f"download_youtube_video({youtubeUrl}) Exception:" + str(e))
            raise e
