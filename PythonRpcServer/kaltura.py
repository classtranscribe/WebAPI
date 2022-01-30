from KalturaClient import *
from KalturaClient.Plugins.Core import *
from urllib.parse import urlparse

import hashlib
import json
import os
from time import perf_counter 


from utils import download_file
from mediaprovider import MediaProvider, InvalidPlaylistInfoException

DATA_DIR = os.getenv('DATA_DIRECTORY')
KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID', default=0))
KALTURA_TOKEN_ID = os.getenv('KALTURA_TOKEN_ID', default=None)
KATLURA_APP_TOKEN = os.getenv('KALTURA_APP_TOKEN', default=None)

if KALTURA_PARTNER_ID == 0 or not KALTURA_TOKEN_ID or not KATLURA_APP_TOKEN:
    print("INVALID KALTURA CREDENTIALS, check KALTURA environment variables.")

# Examples of Playlists URLs the user is likely to see-
# Playlist 1_eilnj5er is Angrave's short set of example vidos
# Mediaspace website generated these
# https://mediaspace.illinois.edu/playlist/dedicated/1_eilnj5er/
# https://mediaspace.illinois.edu/playlist/details/1_eilnj5er
# https://mediaspace.illinois.edu/playlist/edit/1_eilnj5er
# Generating an embedded iframe link at Share and Embed page (https://mediaspace.illinois.edu/playlist/details/1_eilnj5er)
# Generats this
# <iframe src="https://mediaspace.illinois.edu/embedplaylist/secure/embed/v2/1/playlistId/1_eilnj5er/uiConfId/41193381" width="740" height="330" allowfullscreen webkitallowfullscreen mozAllowFullScreen allow="autoplay *; fullscreen *; encrypted-media *" referrerpolicy="no-referrer-when-downgrade" sandbox="allow-forms allow-same-origin allow-scripts allow-top-navigation allow-pointer-lock allow-popups allow-modals allow-orientation-lock allow-popups-to-escape-sandbox allow-presentation allow-top-navigation-by-user-activation" frameborder="0" title="Kaltura Player"></iframe>
# This also works on website ...
# https://mediaspace.illinois.edu/playlist/1_eilnj5er
#
# Example CS105 content playlist organized by weeeks, ('1_ttfygvag' is the playlist
# https://mediaspace.illinois.edu/playlist/dedicated/178284601/0_oghdca8c/0_t9utjsqo
# https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/1_kk4q6ncg
# Video 5 of the same playlist
# https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/1_ll1afddb
# Also works, the 178284601 sis the channel
# https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/
# access denied
# https://mediaspace.illinois.edu/playlist/edit/178284601/1_ttfygvag/
# Also works -
# https://mediaspace.illinois.edu/playlist/1_ttfygvag

# Channels:
# https://mediaspace.illinois.edu/channel/Test%2BVideosB%2B2020_03_09/180228801
# The channel name is ignored if the channel is provided. Otherwise it used to find the channel e.g.
# https://mediaspace.illinois.edu/channel/ignoreme/180228801
# https://mediaspace.illinois.edu/channel/Test%2BVideosB%2B2020_03_09/
# Instructor provided channels -
# https://mediaspace.illinois.edu/channel/channelid/178650472
# https://mediaspace.illinois.edu/channel/CMN+210+%28O%27Gorman%29+Fall+2020/172117521


class KalturaProvider(MediaProvider):
    # limit channel and playlists to 500 videos
    maxTotalEntries = 500

    # Old channel identifiers were just an integer
    DEFAULT_PARTNER_HOST = 'mediaspace.illinois.edu'

    def __init__(self):
        self.client = self.getClient(
            KALTURA_PARTNER_ID, KALTURA_TOKEN_ID, KATLURA_APP_TOKEN)

    # Returns the Kaltura SDK client. Only used internally by constructor
    def getClient(self, partnerId, tokenId, appToken):
        config = KalturaConfiguration(partnerId)
        config.serviceUrl = "https://www.kaltura.com/"
        client = KalturaClient(config)
        # generate a widget session in order to use the app token
        widgetId = "_"+str(partnerId)
        expiry = 864000
        # 864000 = If this is seconds that's about 10 days.
        # It is not in milliseconds (=14.4 minutes); a manual test continued to work after an hour of construction
        # 864000 is the value used in these demos-
        # https://developer.kaltura.com/api-docs/VPaaS-API-Getting-Started/introduction-kaltura-client-libraries.html
        # This suggests it is probably seconds, because 1 second is the min and 10 years is the max
        # https://developer.kaltura.com/api-docs/VPaaS-API-Getting-Started/Kaltura_API_Authentication_and_Security.html

        result = client.session.startWidgetSession(widgetId, expiry)
        client.setKs(result.ks)

        # generate token hash from ks + appToken
        tokenHash = hashlib.sha256(result.ks.encode(
            'ascii')+appToken.encode('ascii')).hexdigest()
        # start an app token session
        result = client.appToken.startSession(
            tokenId, tokenHash, '', '', expiry)
        client.setKs(result.ks)
        return client
    # Returns dict of Media information for a specific media
    # k.getMediaInfo('1_tbxlkewh')
    # {'id': '1_tbxlkewh',
    # 'downloadUrl': 'https://cdnapisec.kaltura.com/p/1329972/sp/132997200/playManifest/entryId/1_tbxlkewh/format/download/protocol/https/flavorParamIds/0',
    # 'name': 'this_is_video_2',
    # 'description': '',
    # 'createdAt': 1599182318}

    def getMediaInfo(self, mediaId):
        mediaEntry = self.client.media.get(mediaId, -1)
        media = {'id': mediaEntry.id,
                 'downloadUrl': mediaEntry.downloadUrl,
                 'name': mediaEntry.name,
                 'description': mediaEntry.description,
                 'createdAt': mediaEntry.createdAt
                 }
        return media

    # For debugging / sanity checks
    def getKalturaAllChannelIds(self):
        channels = self.client.category.list()
        channelIds = [x.id for x in channels.objects]
        return channelIds

    # k.getKalturaChannel(180228801) returns a KalturaClient.Plugins.Core.KalturaCategory

    def getKalturaChannel(self, channelId):
        return self.client.category.get(channelId)

    # k.getMediaEntryIdsForKalturaPlaylist('1_eilnj5er')
    # returns [{ ... attributes for '1_tbxlkewh', '1_wn5kbgqq']
    def getMediaInfosForKalturaPlaylist(self, partnerInfo,  kalturaPlaylistId):
        playlist = self.client.playlist.get(kalturaPlaylistId, -1)
        mediaIds = playlist.getPlaylistContent().split(',')

        return self.getSensibleMediaInfos(mediaIds)

    def getSensibleMediaInfos(self, mediaIds):
        if mediaIds is None:
            return []
        if len(mediaIds) > 500:
            mediaIds = mediaIds[:500]
        infolist = [self.getMediaInfo(id) for id in mediaIds]
        # Drop missing (None) entries
        return [info for info in infolist if info]

    # Channel example - k.getMediaInfosForKalturaChannel(channelId=180228801)
    def getMediaInfosForKalturaChannel(self, partnerInfo, channelId):

        a = KalturaCategoryEntryFilter()
        a.categoryIdEqual = channelId

        pageSize = 50

        # By default only one page of 30 items will be downloaded
        # So we iterating over multiple pages (upto an arbitrary max of 500 items).
        # Fixes https://github.com/classtranscribe/WebAPI/issues/54
        pager = KalturaFilterPager(pageSize=pageSize, pageIndex=1)
        res = []
        while True:
            entries = self.client.categoryEntry.list(a, pager)

            for entry in entries.objects:
                if(entry.entryId and len(entry.entryId) > 0):
                    res.append(entry.entryId)

            if len(res) >= self.maxTotalEntries:
                res = res[0:self.maxTotalEntries]
                break

            if len(entries.objects) < pager.pageSize:
                break

            pager.pageIndex += 1

        return self.getSensibleMediaInfos(res)

    def downloadLecture(self, url):
        filePath, extension = download_file(url)
        return filePath, extension

    #Exxpects
    # request.url - an int (old channel), cannonicalized channel or playlist url
    # Returns server/service namce, boolea (isPlaylist), identifier
    # Identifier is an integer for channels, string for playlists
    def extractKalturalChannelPlaylistResource(self,request):
        try:
            # Old version just the mediaspace.illinois channel number
            if request and request.Url.isdigit():
                return self.DEFAULT_PARTNER_HOST, False, int(request.Url)

            url = urlparse(request.Url)
            servername = url.hostname

            # New Channels now in the form
            # https://host/channel/123456
            if url.path.startswith('/channel/'):
                return servername, False, int(url.path.split('/')[-1])

            # https://host/playlist/1_ttfdv
            if url.path.startswith('/playlist/'):
                return servername, True, url.path.split('/')[-1]
        except Exception as e:
            print("Failed to parse request.Url:" + str(e))
            pass # Fall through

        raise InvalidPlaylistInfoException("Invalid resource:"+request.Url)

    # -------------------------Public API below-----------------
    # Called by PythonServerServicer (server.py) which lightly wraps these methods
    # to provide the RPC implementations GetKalturaChannelEntriesRPC and DownloadKalturaVideoRPC
    #

    # Stub method to be able to support multiple Kaltura partners at the same time
    # We might create a client here based on the derived partner id, based on the hostname provided by the user
    # getMediaInfosForKalturaPlaylist
    #getMediaInfosForKalturaChannel will also need to be updated.
    def getPartnerInfo(self, servername): 
        result = {}
        return result
    

    # Main entry point- overrides stub in MediaProvider
    def getPlaylistItems(self, request):
        # We could be getting a channel or a playlist
        # Ignore Url param if the original URL provided (if known) looks like a Kaltura playlist URL
        # We try a playlist first
        print('getPlaylistItems' + str(request))
        start_time = perf_counter()
        try:
            res = []
            servername, isPlaylist, id = self.extractKalturalChannelPlaylistResource(
                request)
            partnerInfo = self.getPartnerInfo(servername)

            print(f"server={servername},partner= {partnerInfo}, playlist={isPlaylist},id={id}")

            res = self.getMediaInfosForKalturaPlaylist(partnerInfo, id) if isPlaylist else \
                self.getMediaInfosForKalturaChannel(partnerInfo, id)
            print(f'Found {len(res)} items')
            result = json.dumps(res)
        except InvalidPlaylistInfoException as e:
            print(f"getPlaylistItems({request}) Exception:{e}")
            raise e
        except Exception as e:
            print(f"getPlaylistItems({request}) Exception:{e}")
            raise InvalidPlaylistInfoException(
                "Error during Channel/Playlist processing " + str(e))
        end_time = perf_counter()
        print(f"getPlaylistItems({request}) returning '{result}'. Processing ({end_time-start_time:.2f}) seconds.")
        return result

    # Main entry point - overrides stub in MediaProvider super class
    def getMedia(self, request):
        try:
            start_time = perf_counter()
            print(f"getMedia({request}) starting")
            result =  self.downloadLecture(request.videoUrl)
            end_time = perf_counter()
            print(f"getMedia({request}) returning '{result}'. Processing ({end_time-start_time:.2f}) seconds.")

            return result
        except Exception as e:
            print(f"getMedia({request}) Exception:{e}" )
            raise e
