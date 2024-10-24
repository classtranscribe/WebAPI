
import os
if  'DATA_DIRECTORY' not in os.environ:
    os.environ['DATA_DIRECTORY']='.'

import youtube

def test_youtube1():
    print("Test 1/2: Download playlist")
    yt=youtube.YoutubeProvider()
    pl=yt.get_youtube_playlist('PLBgxzZMu3GpPb35BDIU5eeopR4MhBOZw_')
    print(pl)
    #[{'channelId': 'UC4JRDwrS2QC4XNZnSex0Udw', 'playlistId': 'PLBgxzZMu3GpPb35BDIU5eeopR4MhBOZw_', 'title': 'STAT 385 /// Welcome', 'description': 'Course: https://stat385.org/', 'publishedAt': '2021/08/24', 'videoUrl': 'https://youtube.com/watch?v=DqHMh8nqCPw', 'videoId': 'DqHMh8nqCPw', 'createdAt': '2021/08/24'}]
    assert len(pl) >0
    for k in [ 'playlistId', 'title','description','videoUrl','videoId']:
        assert k in pl[0].keys(), f"Expected key {k} in playlist entries"

    assert 'STAT 385' in pl[0]['title']

def test_youtube2():
    print("Test 2/2: Download video")
    yt=youtube.YoutubeProvider()
    onevid = yt.download_youtube_video('https://youtube.com/watch?v=DqHMh8nqCPw') # 24-72 seconds typical
    print(onevid)
    assert len(onevid) == 2

    path, filetype = onevid
    assert filetype == '.mp4'
    # Typical result: ('/PythonRpcServer/./OLYHLMQZ', '.mp4')
    assert os.path.exists(path)
    assert os.stat(path).st_size == 166619691

    print(f"Cleaning up. Removing file {path}")
    os.remove(path)

    print("All tests completed")

if __name__ == "__main__":
    test_youtube1()
    test_youtube2()
