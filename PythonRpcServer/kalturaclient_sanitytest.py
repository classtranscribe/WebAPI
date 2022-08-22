from KalturaClient import *
from KalturaClient.Plugins.Core import *
import sys,os

import hashlib
import json
import os

KALTURA_PARTNER_ID = int(os.getenv('KALTURA_PARTNER_ID', default=0))
KALTURA_TOKEN_ID = os.getenv('KALTURA_TOKEN_ID', default=None)
KATLURA_APP_TOKEN = os.getenv('KALTURA_APP_TOKEN', default=None)

if KALTURA_PARTNER_ID == 0 or not KALTURA_TOKEN_ID or not KATLURA_APP_TOKEN:
    print("INVALID KALTURA CREDENTIALS, check KALTURA environment variables.")
    assert False


EXAMPLE_PUBLIC_CHANNEL_ID = 180228801

#https://mediaspace.illinois.edu/media/t/1_8r2lwupy/9427081
#https://mediaspace.illinois.edu/media/t/1_wzi6u8mt/9427081
EXAMPLE_PUBLIC_MEDIA_ID='1_wzi6u8mt'

# https://mediaspace.illinois.edu/channel/CS%2B341%2B2022%2BFall/266843702
MY_CHANNEL_ID = 266843702
#https://mediaspace.illinois.edu/media/t/1_hqgd9bua
MY_MEDIA_ID = '1_hqgd9bua'


def createClient(partnerId, tokenId, appToken):
    config = KalturaConfiguration(partnerId)
    config.serviceUrl = "https://www.kaltura.com/" #"https://mediaspace.illinois.edu/" # "https://www.kaltura.com/"
    client = KalturaClient(config)
    
    widgetId = "_"+str(partnerId)
    expiry = 864000
    print("Starting Widget Session")
    result = client.session.startWidgetSession(widgetId, expiry)
    client.setKs(result.ks)
    print('startWidgetSession Complete.')
    #tokenHash = hashlib.sha256(result.ks.encode('ascii') + appToken.encode('ascii')).hexdigest()
    
    #print("Starting appToken session")
    #result = client.appToken.startSession( tokenId, tokenHash, '', '', expiry) # Fails here
    #client.setKs(result.ks)
    
    return client

def getMediaInfo(client, mediaId):
    print(f"Requesting {mediaId}")
    mediaEntry = client.media.get(mediaId, -1)
    info = {'id': mediaEntry.id,
    'downloadUrl': mediaEntry.downloadUrl,
    'name': mediaEntry.name,
    'description': mediaEntry.description,
    'createdAt': mediaEntry.createdAt
    }
    print(info)
    return info


def getChannelInfo(client, channelId):
    print(f"Requesting {channelId}")
    channel = client.category.get(channelId)
    
    info = {'id': channel.id, 'name': channel.name, 'createdAt' : channel.createdAt, 'description': channel.description}
    print(info)
    return info

def testKalturaClientAPI():
    print("Creating Kaltura client...")
    client = createClient(KALTURA_PARTNER_ID, KALTURA_TOKEN_ID, KATLURA_APP_TOKEN)

    info1 = getChannelInfo(client, EXAMPLE_PUBLIC_CHANNEL_ID) 
    info2 = getMediaInfo(client, EXAMPLE_PUBLIC_MEDIA_ID)
    info3 = getChannelInfo(client, MY_CHANNEL_ID)     
    info4 = getMediaInfo(client, MY_MEDIA_ID)
   
    expected1 = {'id': 180228801, 'name': 'Test VideosB 2020_03_09', 'createdAt': 1599182079, 'description': ''};
    expected2 = {'id': '1_wzi6u8mt', 'downloadUrl': 'https://cdnapisec.kaltura.com/p/1329972/sp/132997200/playManifest/entryId/1_wzi6u8mt/format/download/protocol/https/flavorParamIds/0', 'name': 'Computer-Based Education', 'description': 'PLATO, Computer Based Education', 'createdAt': 1361898391}
    expected3={'id': 266843702, 'name': 'CS 341 2022 Fall', 'createdAt': 1660588552, 'description': 'CS\n341 2022 Fall'}
    expected4 = {'id': '1_hqgd9bua', 'downloadUrl': 'https://cdnapisec.kaltura.com/p/1329972/sp/132997200/playManifest/entryId/1_hqgd9bua/format/download/protocol/https/flavorParamIds/0', 'name': 'The Language Of - And People Of - Computer Science', 'description': 'Language And People Of Computer Science - an interview with Prof. Tiffani Williams', 'createdAt': 1651624658}

    assert info1 == expected1
    assert info2 == expected2
    assert info3 == expected3
    assert info4 == expected4
    
    print("Finished")

if __name__ == '__main__' :
   testKalturaClientAPI()
