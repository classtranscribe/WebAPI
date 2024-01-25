import pickle
import codecs
import string
import random
import requests
import os
import requests
import mimetypes

## CAUTION ##
# When imported this file seeds the RNG with random bytes from the os.urandom() - see below

# Max number of threads this process should use. Be careful about setting this value too high
# Unless you enjoy bringing a production machine to a crawl and debugging timeouts
# Recommended values 1-4
DEFAULT_JOB_MAX_THREAD = 4

DATA_DIRECTORY = os.getenv('DATA_DIRECTORY')


# Returns the preferred maximum number of threads to use
def getMaxThreads():
    return int(os.getenv('JOB_MAX_THREADS', DEFAULT_JOB_MAX_THREAD))


def encode(obj):
    return codecs.encode(pickle.dumps(obj), "base64").decode()


def decode(pickled):
    return pickle.loads(codecs.decode(pickled.encode(), "base64"))


# We could add lowercase, but since this is used for filenames and some filesystems are case insensitve, so let's not.
# Excluding 1 and 0 (too visually to similar to IO)
def getRandomString(n):
    return ''.join(random.choices(string.ascii_uppercase + "23456789", k=n))


# Ensure two python processes started at the same time, are seeded with different values
# reduces likelihood of two random filename collision
random.seed(os.urandom(8))

# Returns a path to a file that does not exit.
# There is a use-after-check race condition that two processes might randomly choose the same file path
# And the could be greater than the improbably 2 in 34^12 if two process RNG were identically seeded
# e.g. were seeded by an identical time.
# Thus each process pulls bytes from os.urandom to see python's RNG
# See random.seed - in this file


def getTmpFile(subdir="pythonrpc"):
    os.makedirs(os.path.join(DATA_DIRECTORY, subdir), exist_ok=True)
    while True:
        # A key space of 34^12 should be sufficient for us...
        filenameSize = 12
        candidate = os.path.join(DATA_DIRECTORY, subdir, "tmp_"+getRandomString(filenameSize))
        if not os.path.exists(candidate):
            return candidate
        # We wil never print this, and if we do, no-one will read it.
        # The loop exists purely for reasoning about the code
        print("This loop so precious; Unrun unlogged yet forces; The bug is elsewhere.")

# See https://www.garykessler.net/library/file_sigs.html
# Todo extension for Apple new HEIV format?
def extension_from_magic_bytes(filepath):
    bytes = ''
    try:
        with open(filepath,'rb') as f:
            bytes = f.read(20).hex()  
    except Exception:
        pass
    first4 = bytes[0:8]
    skip4 = bytes[8:]

    common_mp4 = [b'ftypqt',b'ftypmp42',b'ftypisom',b'ftypMSNV',b'ftypM4V']
    for check in common_mp4:
        if skip4.startswith(check.hex()):
            return '.mp4'

    if skip4.startswith('6674797071742020'): return '.mov'   
    if first4[:7] == '000001B' : return '.mpg'
    
    return ''

# Downloads given URL to the specified filepath (or if unspecified, a random filename)
# Filepath and cookies may be specified
# Returns a two tuple, [filepath,  extension]
# An appropriate Extension is guessed based on the mimetype in the 'content-type' response header
def download_file(url, filepath=None, cookies=None):
    # NOTE the stream=True parameter below
    if not filepath:
        filepath = getTmpFile()
    extension = None
    with requests.get(url, stream=True, allow_redirects=True, cookies=cookies) as r:
        
        
        r.raise_for_status()

        extension = ''
        if 'content-type' in r.headers:
            extension = mimetypes.guess_extension(r.headers['content-type'])
        
        with open(filepath, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                if chunk:  # filter out keep-alive new chunks
                    f.write(chunk)
                    # f.flush()

        if extension == '':
            extension = extension_from_magic_bytes(filepath)
    
    return filepath, extension
