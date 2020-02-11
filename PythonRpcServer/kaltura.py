from KalturaClient import *
from KalturaClient.Plugins.Core import *
import hashlib
import json
import os

DATA_DIR = os.getenv('DATA_DIRECTORY')
KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID'))
KALTURA_TOKEN_ID = os.getenv('KALTURA_TOKEN_ID')
KATLURA_APP_TOKEN = os.getenv('KALTURA_APP_TOKEN')

class Kaltura:
    def __init__(self):
        self.client = self.getClient(KALTURA_PARTNER_ID, KALTURA_TOKEN_ID, KATLURA_APP_TOKEN)


    def getClient(self, partnerId, tokenId, appToken):
        config = KalturaConfiguration(partnerId)
        config.serviceUrl = "https://www.kaltura.com/"
        client = KalturaClient(config)
        # generate a widget session in order to use the app token
        widgetId = "_"+str(partnerId)
        expiry = 864000
        result = client.session.startWidgetSession(widgetId, expiry)
        client.setKs(result.ks)    
        
        # generate token hash from ks + appToken
        tokenHash = hashlib.sha256(result.ks.encode('ascii')+appToken.encode('ascii')).hexdigest()    
        # start an app token session
        result = client.appToken.startSession(tokenId, tokenHash, '', '', expiry)
        client.setKs(result.ks)    
        return client

    def getMediaInfo(self, mediaId):
        mediaEntry = self.client.media.get(mediaId, -1)
        media = {'id': mediaEntry.id, 
                    'downloadUrl': mediaEntry.downloadUrl, 
                    'name': mediaEntry.name,
                    'description': mediaEntry.description,
                    'createdAt': mediaEntry.createdAt
                }
        return media

    def getKalturaPlaylist(self, kalturaPlaylistId):
        playlist = self.client.playlist.get(kalturaPlaylistId, -1)
        mediaIds = playlist.getPlaylistContent().split(',')
        return mediaIds

    def getKalturaAllChannelIds(self):
        channels = self.client.category.list()
        channelIds = [x.id for x in channels.objects]
        return channelIds

    def getKalturaChannel(self, channelId):
        return self.client.category.get(channelId)
        
    def getKalturaChannelEntries(self, channelId):    
        a = KalturaCategoryEntryFilter()
        a.categoryIdEqual = channelId
        b = KalturaFilterPager()    
        entries = self.client.categoryEntry.list(a, b)    
        res = []
        for entry in entries.objects:
            res.append(self.getMediaInfo(entry.entryId))    
        return json.dumps(res)