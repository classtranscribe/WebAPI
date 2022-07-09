from pytube.extract import playlist_id
import requests
from utils import encode, decode, getRandomString, download_file
import os
import json
from time import perf_counter

#from pytube import YouTube
import pytube

from mediaprovider import MediaProvider, InvalidPlaylistInfoException

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY', 'DATA_DIRECTORY')
assert(DATA_DIRECTORY)

#YOUTUBE_API_KEY = os.getenv('YOUTUBE_API_KEY')
#YOUTUBE_PLAYLIST_URL = 'https://www.googleapis.com/youtube/v3/playlistItems'
#YOUTUBE_CHANNELS_URL = 'https://www.googleapis.com/youtube/v3/channels'
YOUTUBE_PLAYLIST_BASE_URL = 'https://www.youtube.com/playlist?list='
YOUTUBE_CHANNEL_BASE_URL = 'https://www.youtube.com/channel/'


class YoutubeProvider(MediaProvider):

    def getPlaylistItems(self, request):
        print(f'getPlaylistItems({request})')
        isChannel = False

        try:
            meta = json.loads(request.metadata.json)
            isChannel = meta['isChannel'] == '1'
        except:
            pass  # Missing key, json is null etc

        medias = self.get_youtube_channel(
            request.Url) if isChannel else self.get_youtube_playlist(request.Url)

        return json.dumps(medias)

    def getMedia(self, request):
        return self.download_youtube_video(request.videoUrl)

    def get_youtube_channel(self, identifier):
        print(f'get_youtube_channel({identifier})')

        url = YOUTUBE_CHANNEL_BASE_URL + identifier
        channel = pytube.Channel(url)

        playlist_id = channel.playlist_id
        # according to one StackOver and one test, channels-to-playlists can also be converted with string replace  UCXXXX to UUXXXX
        print(f"channel {identifier}-> playlist {playlist_id}")
        return self.get_youtube_playlist(playlist_id)

    def get_youtube_playlist(self, identifier):
        try:
            start_time = perf_counter()

            url = YOUTUBE_PLAYLIST_BASE_URL + identifier
            print(f"get_youtube_playlist(identifier): {url}")
            playlist = pytube.Playlist(url)

            medias = []
            for v in playlist.videos:

                published_at = v.publish_date.strftime('%Y/%m/%d')
                media = {
                    # "channelTitle": channelTitle,
                    "channelId": v.channel_id,
                    "playlistId": identifier,
                    "title": v.title,
                    "description": v.description,
                    "publishedAt": published_at,
                    "videoUrl": v.watch_url,
                    "videoId": v.video_id,
                    "createdAt": published_at
                }
                medias.append(media)
            end_time = perf_counter()
            print(
                f'Youtube playlist {identifier}: Returning {len(medias)} items. Processing time {end_time - start_time :.2f} seconds')
            return medias
        except Exception as e:
            print("get_youtube_playlist({request}) Exception:" + str(e))
            raise e

    def download_youtube_video(self, youtubeUrl):
        try:
            print(f"download_youtube_video({youtubeUrl}): Starting")
            start_time = perf_counter()
            extension = '.mp4'
            filename = getRandomString(8)
            filepath = pytube.YouTube(youtubeUrl).streams.filter(subtype='mp4').get_highest_resolution(
            ).download(output_path=DATA_DIRECTORY, filename=filename)
            end_time = perf_counter()
            print(
                f"download_youtube_video({youtubeUrl}): Done. Downloaded in {end_time - start_time :.2f} seconds")
            return filepath, extension
        except Exception as e:
            print("download_youtube_video({request}) Exception:" + str(e))
            raise e
