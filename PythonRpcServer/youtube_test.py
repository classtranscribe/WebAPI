

import urllib.request
with urllib.request.urlopen('http://python.org/') as response:
       html = response.read()
       print(html[:100])

from pytube import YouTube
# From https://pytube.io/en/latest/user/quickstart.html
yt = YouTube('http://youtube.com/watch?v=2lAe1cqCOXo')

print(yt.title) # YouTube Rewind 2019: For the Record | #YouTubeRewind