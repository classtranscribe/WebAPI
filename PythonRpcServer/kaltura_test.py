import ct_pb2
import kaltura
import json

# Example playlist
# https://mediaspace.illinois.edu/playlist/details/0_oghdca8c/categoryid/178284601


def testKaltura():
    k = kaltura.KalturaProvider()

    request = ct_pb2.PlaylistRequest()

    request.Url = 42
    servername, isPlaylist, id = k.extractKalturalChannelPlaylistResource(
        request)
    assert(servername == 'mediaspace.illinois.edu')
    assert(isinstance(isPlaylist, bool) and not isPlaylist)
    assert(isinstance(id, int) and id == 42)

    request.Url = 'https://mediaspace.illinois.edu/playlist/1_rfz3i45g'
    servername, isPlaylist, id = k.extractKalturalChannelPlaylistResource(
        request)
    assert(servername == 'mediaspace.illinois.edu')
    assert(isinstance(isPlaylist, bool) and isPlaylist)
    assert(id == '1_rfz3i45g')

    request.Url = 'https://mediaspace.illinois.edu/channel/123'
    servername, isPlaylist, id = k.extractKalturalChannelPlaylistResource(
        request)
    assert(servername == 'mediaspace.illinois.edu')
    assert(isinstance(isPlaylist, bool) and not isPlaylist)
    assert(isinstance(id, int) and id == 123)


def nolongervalidtestPlaylists():
    k = kaltura.KalturaProvider()

    playlisturl = 'https://mediaspace.illinois.edu/playlist/details/1_rfz3i45g'

    # See ct.proto
    request = ct_pb2.PlaylistRequest()
    request.Url = '42'  # should be ignored in preference for the playlist information
    request.metadata.json = '{"source":"' + playlisturl + '"}'

    result = json.loads(k.getPlaylistItems(request))
    assert len(result) == 3


def testChannels():
    k = kaltura.KalturaProvider()

    channelurl = 'https://mediaspace.illinois.edu/channel/Test%2BVideosB%2B2020_03_09/180228801'

    # See ct.proto
    request = ct_pb2.PlaylistRequest()
    request.Url = channelurl  # should be ignored
    request.metadata.json = '{"source":"' + channelurl + '"}'

    result = json.loads(k.getPlaylistItems(request))
    assert len(result) == 2

    # Old style Channel
    request = ct_pb2.PlaylistRequest()
    request.Url = '180228801'
    request.metadata.json = ''

    result = json.loads(k.getPlaylistItems(request))
    assert len(result) == 2

#       # https://mediaspace.illinois.edu/channel/CMN+210+%28O%27Gorman%29+Fall+2020/172117521

    # /channel/channelid/178650472

#     def extractKalturaPlaylistIdentifer(request=None, teststring=None):
#         # Sanity check does the source URL start with playlist?
#         # If not return None
#         try:
#             meta = json.loads(request.metadata.json if request else teststring )
#             # look for https:/.../playlist/

#             urlpath = urlparse(meta['source'] ).path

#             if not urlpath.startswith('/playlist/'):
#                 return None
#         except Exception:
#             return None # Missing key, json is null etc

#         # Next extract the playlist Id; it could be in different places (see examples at the top of this file)
#         try:
#             # Incase someone shortens
#             # https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/....
#             # to
#             #https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/
#             urlpath = urlpath.rstrip('/') # Unnecessary / at the end will not work with the logic below

#             path = urlpath.split('/')[1:] # skip the empty '' at the start
#             assert path[0] == 'playlist'
#             # e.g. [ 'playlist', 'dedicated', '178284601', '1_ttfygvag', '1_kk4q6ncg']
#             parts = len(path)
#             if parts <= 1: raise Exception() # No id

# #             e.g. https://mediaspace.illinois.edu/playlist/1_ttfygvag
#             if parts == 2 :
#                 return path[-1]

#             # https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/1_kk4q6ncg
#             if(parts>=4 and path[1] in ['dedicated']) : # Skip over the channel id (and we don't want the video id at the end)
#                 return path[3]

#             # For len 3 or 4, take the last item
#             # e.g.  https://mediaspace.illinois.edu/playlist/details/1_ttfygvag
#             if(parts <= 4):
#                 return path[-1]

#             return path[2]
#         except:
#             pass # We will generate an exception below

#         raise InvalidPlaylistInfoException("Invalid playlist,"+ urlpath)


#     # manual test
#     def testExtractKalturaPlaylist(self):
#         examples = ['https://mediaspace.illinois.edu/playlist/1_ttfygvag',
#         'https://mediaspace.illinois.edu/playlist/dedicated/178284601/1_ttfygvag/1_kk4q6ncg']
#         for url  in examples:
#             jsonstring = '{"source":"' + url + '"}'
#             assert( self.extractKalturaPlaylistIdentifer(teststring=jsonstring) == '1_ttfygvag')
