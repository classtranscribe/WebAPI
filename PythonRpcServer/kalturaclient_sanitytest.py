from KalturaClient import *
from KalturaClient.Plugins.Core import *

import hashlib
import os, sys
import requests

KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID', default=0))
KALTURA_TOKEN_ID = os.getenv('KALTURA_TOKEN_ID', default=None)
KATLURA_APP_TOKEN = os.getenv('KALTURA_APP_TOKEN', default=None)

if KALTURA_PARTNER_ID == 0 or not KALTURA_TOKEN_ID or not KATLURA_APP_TOKEN:
    print("INVALID KALTURA CREDENTIALS, check KALTURA environment variables.")
    assert False
KS = ''

# https://forum.kaltura.org/t/what-is-proper-expiry-value-when-invoking-client-apptoken-startsession/11329

def createClient(partnerId, tokenId, appToken):
    config = KalturaConfiguration(partnerId)
    config.serviceUrl = "https://cdnapisec.kaltura.com" # "https://www.kaltura.com/" #"https://mediaspace.illinois.edu/" # "https://www.kaltura.com/"
    client = KalturaClient(config)
    
    widgetId = "_"+str(partnerId)
    expiry = 864000
    print("Starting Widget Session")
    result = client.session.startWidgetSession(widgetId, expiry)
    client.setKs(result.ks)
    print('startWidgetSession Complete.')
    
    tokenHash = hashlib.sha256(result.ks.encode('ascii') + appToken.encode('ascii')).hexdigest()
    
    print("Starting appToken session")
    result = client.appToken.startSession( tokenId, tokenHash, '', '', expiry) # Fails here
    # ^ throws APP_TOKEN_ID_NOT_FOUND
    
    client.setKs(result.ks)
    global KS
    KS = result.ks
    
    return client

def getMediaInfo(client, mediaId):
    print(f"Requesting {mediaId}")
    mediaEntry = client.media.get(mediaId, -1)
    #dir(mediaEntry)
    info = {'id': mediaEntry.id,
        'downloadUrl': mediaEntry.downloadUrl,
        'name': mediaEntry.name,
        'description': mediaEntry.description,
        'duration' : mediaEntry.duration,
        'createdAt': mediaEntry.createdAt,
        'mediaType': mediaEntry.mediaType.value,
        'parentEntryId' : mediaEntry.parentEntryId
    }
    print(info)
    return info


def getChannelInfo(client, channelId):
    print(f"Requesting {channelId}")
    channel = client.category.get(channelId)
    
    info = {'id': channel.id, 'name': channel.name, 'createdAt' : channel.createdAt, 'description': channel.description}
    print(info)
    return info

def getPlaylistEntries(client,playlistId):
    print(f"Listing Playlist playlistId")
    p = client.playlist.get(playlistId)
    print( p.playlistContent ) 
    return p.playlistContent

    
def getChannelEntries(client, channelId):
    print(f"Listing Channel {channelId}")
    filter = KalturaCategoryEntryFilter()
    filter.categoryIdEqual = channelId
  
    pager = KalturaFilterPager(pageSize=30, pageIndex=1)
    res = []
    
    entries = client.categoryEntry.list(filter, pager)
    #entries = client.media.list(filter, pager)
    for entry in entries.objects:
        if(entry.entryId and len(entry.entryId) > 0):
                res.append( entry.entryId)

    print(res)
    return res

# https://knowledge.kaltura.com/help/how-to-retrieve-the-download-or-streaming-url-using-api-calls
# https://github.com/kaltura/DeveloperPortalDocs/blob/master/documentation/Deliver-and-Distribute-Media/how-retrieve-download-or-streaming-url-using-api-calls.md
    

def downloadFile(url, filepath=None, cookies=None):
    extension = None
    count = 0
    maxcount = 256
    print(url, filepath)
    with requests.get(url, stream=True, allow_redirects=True, cookies=cookies) as r:        
        with open(filepath, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                if chunk:  # filter out keep-alive new chunks
                    f.write(chunk)
                    
                    if( count % 32 == 0):
                        sys.stderr.write('.')
                    count +=1
                    if (count >= maxcount):
                        break
                        
    print(f"{count} chunks downloaded")
                    
                    
def downloadMedia(client,entryId):
    print(f'downloadMedia {entryId}')
    global KS
    entryId = entryId.replace('/','')
    serviceUrl = 'https://cdnapisec.kaltura.com'
    partnerId = KALTURA_PARTNER_ID
    streamingFormat= 'download' #'url'
    protocol= 'https'
    videoFlavorId= 0
    ks= KS
    ext = 'mp4'
    url= f"{serviceUrl}/p/{partnerId}/sp/0/playManifest/entryId/{entryId}/format/{streamingFormat}/protocol/{protocol}/flavorParamId/{videoFlavorId}/ks/{ks}/video.{ext}"

    downloadFile(url,f"{entryId}.mp4")
    
    
def testKalturaClientAPI():
    print("Creating Kaltura client...")
    client = createClient(KALTURA_PARTNER_ID, KALTURA_TOKEN_ID, KATLURA_APP_TOKEN)
    channelEntries = getChannelEntries(client, 266843702)
    for m in channelEntries:
        media = getMediaInfo(client, m)
    
def test2KalturaClientAPI():

    print("Creating Kaltura client...")
    client = createClient(KALTURA_PARTNER_ID, KALTURA_TOKEN_ID, KATLURA_APP_TOKEN)
    channelEntries = getChannelEntries(client, MY_CHANNEL_ID)
    for m in channelEntries:
        media = getMediaInfo(client, m)
        
    MY_CHANNEL_ID = 266843702
    #  266843702 - CS341 FA22 channel
    # 260743952  - CS361 FA22
    # 268264992 

    MY_MEDIA_ID = '1_hqgd9bua'
    MY_PLAYLIST_ID = '1_rfz3i45g'
    # CS361 : 1_4cfiixlg , 1_kj8wyyn4
    downloadMedia(client,'1_4cfiixlg') # CS361 but zero bytes?
    downloadMedia(client,'1_kj8wyyn4') # CS361
    
    # CS341 FA22 ['1_nvd1xkx1', '1_1ldhqtpc', '1_d73kf02r']
    # 1_d73kf02r
    #1_1ldhqtpc
    downloadMedia(client,'1_d73kf02r') # Cs341
    downloadMedia(client,'1_1ldhqtpc') # Cs341
    

    channelEntries = getChannelEntries(client, MY_CHANNEL_ID)
    for m in channelEntries:
        media = getMediaInfo(client, m)
    return 
    playlistEntries =  getPlaylistEntries(client, MY_PLAYLIST_ID )
    
    info3 = getChannelInfo(client, MY_CHANNEL_ID)     
    info4 = getMediaInfo(client, MY_MEDIA_ID)
   
    assert playlistEntries == '1_all1hnme,1_14vd1lkm,1_wn5kbgqq'
    assert len(channelEntries) > 0
    #assert info3 == {'id': 266843702, 'name': 'CS 341 2022 Fall', 'createdAt': 1660588552, 'description': 'CS\n341 2022 Fall'}
    assert info4 == {'id': '1_hqgd9bua', 'downloadUrl': 'https://cdnapisec.kaltura.com/p/1329972/sp/132997200/playManifest/entryId/1_hqgd9bua/format/download/protocol/https/flavorParamIds/0', 'name': 'The Language Of - And People Of - Computer Science', 'description': 'Language And People Of Computer Science - an interview with Prof. Tiffani Williams', 'createdAt': 1651624658}, info4
    
    print("Finished")

if __name__ == '__main__' :
   testKalturaClientAPI()
