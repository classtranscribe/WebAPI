import hashlib

#eventually this may replace the C# method
# However today the C# file hash is calculated inside the Database project, which does not depende on the RPC projec

# This implementation can be extended to create multiple digests using a single read through the file
# A major advantage of calculating this in python is that we can perform this under ionice and nice(cpu) constraints
def hashFile(filepath, algorithms):

    if algorithms != "sha256" :
        raise Exception(f"digest not yet implemented: alg=({algorithms})")

    sha256 = hashlib.sha256()

    blocksize = 64 * 1024

    with open(filepath, 'rb') as f:
        while True:
            block = f.read(blocksize)
            if not block:
                break
            sha256.update(block)
   
    return sha256.hexdigest()
