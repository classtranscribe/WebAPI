class MediaProvider:
    def getPlaylistItems(self, request):
        raise NotImplementedError("To be implemented")

    def getMedia(self, request):
        raise NotImplementedError("To be implemented")

class InvalidPlaylistInfoException(Exception):
    pass
