class MediaProvider:
    def getPlaylistItems(self, request):
        raise NotImplementedError("To be implemented")

    def getMedia(self, request):
        raise NotImplementedError("To be implemented")

class InvalidPlaylistInfoException(Exception):
    def __init__(self, message = 'INVALID_PLAYLIST_IDENTIFIER'):
        self.message = message

