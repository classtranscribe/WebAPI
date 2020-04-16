import requests
from utils import encode, decode, getRandomString, download_file
import os
from pytube import YouTube
import json
 

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY')
YOUTUBE_API_KEY = os.getenv('YOUTUBE_API_KEY')

YOUTUBE_BASE_URL = 'https://www.googleapis.com/youtube/v3/playlistItems'

def get_youtube_playlist(playlistIdentifier):
    # LIMITATION: Can download a maximum of 50 videos per playlist.
    request1 = requests.get(YOUTUBE_BASE_URL,  
    params =  {        
        'part': 'snippet',
        'playlistId': playlistIdentifier,
        'key': YOUTUBE_API_KEY,
        'maxResults': 50
    })

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
    return json.dumps(medias)

def download_youtube_video(youtubeUrl):
    extension = '.mp4'
    filename = getRandomString(8)
    filepath = YouTube(youtubeUrl).streams.filter(subtype='mp4').get_highest_resolution().download(output_path = DATA_DIRECTORY, filename = filename)
    return filepath, extension
