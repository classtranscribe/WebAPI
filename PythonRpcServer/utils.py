import pickle
import codecs
import string
import random
import requests
import os
import requests
import mimetypes

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY')

def encode(obj):
    return codecs.encode(pickle.dumps(obj), "base64").decode()
def decode(pickled):
    return pickle.loads(codecs.decode(pickled.encode(), "base64"))

def getRandomString(n):
    return ''.join(random.choices(string.ascii_uppercase + string.digits, k=n))

def getTmpFile():
    return os.path.join(DATA_DIRECTORY, getRandomString(8))

def download_file(url, filepath = None, cookies = None):
    # NOTE the stream=True parameter below
    if not filepath:
        filepath = getTmpFile()
    extension = None
    with requests.get(url, stream=True, allow_redirects=True, cookies=cookies) as r:
        extension = mimetypes.guess_extension(r.headers['content-type'])
        r.raise_for_status()
        with open(filepath, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192): 
                if chunk: # filter out keep-alive new chunks
                    f.write(chunk)
                    # f.flush()
    return filepath, extension