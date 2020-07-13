from KalturaClient import *
from KalturaClient.Plugins.Core import *
import hashlib
import json
import os
from utils import download_file
from mediaprovider import MediaProvider, InvalidPlaylistInfoException

DATA_DIR = os.getenv('DATA_DIRECTORY')
KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID', default = 0))
KALTURA_TOKEN_ID = os.getenv('KALTURA_TOKEN_ID', default = None)
KATLURA_APP_TOKEN = os.getenv('KALTURA_APP_TOKEN', default = None)

if KALTURA_PARTNER_ID == 0 or not KALTURA_TOKEN_ID or not KATLURA_APP_TOKEN:
    print("INVALID KALTURA CREDENTIALS, check KALTURA environment variables.")

class KalturaProvider(MediaProvider):
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

        pageSize = 50
        maxTotalEntries = 500
        # By default only one page of 30 items will be downloaded
        # So we iterating over multiple pages (upto an arbitrary max of 500 items). 
        # Fixes https://github.com/classtranscribe/WebAPI/issues/54
        pager = KalturaFilterPager(pageSize=pageSize,pageIndex=1)
        res = []
        while True:
            entries = self.client.categoryEntry.list(a, pager)    
        
            for entry in entries.objects:
                res.append(self.getMediaInfo(entry.entryId))

            if len(res) >= maxTotalEntries:
                res = res[0:maxTotalEntries]
                break

            if len(entries.objects) < pager.pageSize:
                break

            pager.pageIndex += 1

        return res
    
    def downloadLecture(self, url):        
        filePath, extension = download_file(url)
        return filePath, extension

    def getPlaylistItems(self, request):
        channelId = int(request.Url)
        try:
            channel = self.getKalturaChannel(channelId)
        except Exception as e:
            raise InvalidPlaylistInfoException(e.message)

        res = self.getKalturaChannelEntries(channelId)
        return json.dumps(res)
    
    def getMedia(self, request):
        return self.downloadLecture(request.videoUrl)
