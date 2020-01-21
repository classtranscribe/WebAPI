from KalturaClient import *
import json
import os

DATA_DIR = os.getenv('DATA_DIRECTORY')
KALTURA_ADMIN_SECRET = os.getenv('KALTURA_ADMIN_SECRET')
KALTURA_USER_ID = os.getenv('KALTURA_USER_ID')
KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID'))

config = KalturaConfiguration()
client = KalturaClient(config)

def getKalturaPlaylist(kalturaPlaylistId):
    ks = client.session.start(
    KALTURA_ADMIN_SECRET,
    KALTURA_USER_ID,
    Plugins.Core.KalturaSessionType.ADMIN,
    KALTURA_PARTNER_ID,
    86400, 
    "appId:appName-appDomain") 

    client.setKs(ks)

    playlist = client.playlist.get(kalturaPlaylistId, -1)
    mediaIds = playlist.getPlaylistContent().split(',')

    res = []

    for mediaId in mediaIds:
        print(mediaId)
        mediaEntry = client.media.get(mediaId, -1)
        res.append({'id': mediaEntry.id, 
                    'downloadUrl': mediaEntry.downloadUrl, 
                    'name': mediaEntry.name,
                    'description': mediaEntry.description,
                    'createdAt': mediaEntry.createdAt
                     })

    return json.dumps(res)