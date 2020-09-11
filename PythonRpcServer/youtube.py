import requests
from utils import encode, decode, getRandomString, download_file
import os
import json
from pytube import YouTube

from mediaprovider import MediaProvider, InvalidPlaylistInfoException

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY')
YOUTUBE_API_KEY = os.getenv('YOUTUBE_API_KEY')
YOUTUBE_PLAYLIST_URL = 'https://www.googleapis.com/youtube/v3/playlistItems'
YOUTUBE_CHANNELS_URL = 'https://www.googleapis.com/youtube/v3/channels'


class YoutubeProvider(MediaProvider):

    def getPlaylistItems(self, request):
        #print('getPlaylistItems'+str(request))
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
        print('get_youtube_channel')
        request1 = requests.get(YOUTUBE_CHANNELS_URL, params={
                                'part': 'contentDetails', 'id': identifier, 'key': YOUTUBE_API_KEY})
        if request1.status_code == 404 or request1.status_code == 500:
            raise InvalidPlaylistInfoException
        else:
            request1.raise_for_status()

        playlistId = request1.json(
        )['items'][0]['contentDetails']['relatedPlaylists']['uploads']
        #according to one StackOver and one test, channels-to-playlists can also be converted with string replace  UCXXXX to UUXXXX
        return self.get_youtube_playlist(playlistId)

    def get_youtube_playlist(self, identifier):
        print('get_youtube_playlist' + str(identifier))
        # Documented API LIMITATION: Can download a maximum of 50 videos per playlist.
        # https://developers.google.com/youtube/v3/docs/playlistItems/list
        request1 = requests.get(YOUTUBE_PLAYLIST_URL,
            params={
            'part': 'snippet',
            'playlistId': identifier,
            'key': YOUTUBE_API_KEY,
            'maxResults': 50
            })

        if request1.status_code == 404 or request1.status_code == 500:
            raise InvalidPlaylistInfoException
        else:
            request1.raise_for_status()

        medias = []
        items = request1.json()['items']
        for item in items:
            publishedAt = item['snippet']['publishedAt']
            channelId = item['snippet']['channelId']
            title = item['snippet']['title']
            description = item['snippet']['description']
            channelTitle = item['snippet']['channelTitle']
            playlistId = item['snippet']['playlistId']
            videoId = item['snippet']['resourceId']['videoId']
            videoUrl = 'http://www.youtube.com/watch?v=' + videoId
            media = {
                "channelTitle": channelTitle,
                "channelId": channelId,
                "playlistId": playlistId,
                "title": title,
                "description": description,
                "publishedAt": publishedAt,
                "videoUrl": videoUrl,
                "videoId": videoId,
                "createdAt": publishedAt
            }
            medias.append(media)
        
        return medias        

    def download_youtube_video(self, youtubeUrl):
        extension = '.mp4'
        filename = getRandomString(8)
        filepath = YouTube(youtubeUrl).streams.filter(subtype='mp4').get_highest_resolution().download(output_path = DATA_DIRECTORY, filename = filename)
        return filepath, extension
