import re
import operator

from string import ascii_letters, digits
from collections import Counter,defaultdict
from nltk.corpus import brown,stopwords
#from nltk.stem.wordnet import WordNetLemmatizer
from prefixspan import PrefixSpan



# Work with phrases that we have extracted per scene to create useful phrase list for speech recognition

# Input - list of strings (candidate phrases) from OCR
# Output: list of phrases for speech recognition

# Setup: 
# pip install nltk

# The stopwords and Brown corpus must be manually downloaded,
#  python -m nltk.downloader brown
#  python -m nltk.downloader stopwords

# Based on original code at https://github.com/lijiaxi2018/advanced-speech-recognition
# Created as part of Undergraduate Project for Angrave for Spring 2021
def delete_inplace_unwanted_characters(wordCountDict):
    """
    A function that cleans up in place each individual word dictionary in wordCountDict, and leaving only numbers and ascii letters
    """

    # Filtering -- leaving just numbers and letters
    allowed = ascii_letters + digits

    # Use list() to create a copy of the keys so we can iterate while deleting
    for key in list(wordCountDict):
        if set(key).difference(allowed):
            wordCountDict.pop(key)

def filter_stop_words(phraseList):
    """
    A function to remove the common words
    """

    stop_words = get_stop_words_set()
    output = [ w for w in phraseList if w.lower() not in stop_words]

    return output

_brown_corpus_count = None

def get_brown_corpus_count():
    global _brown_corpus_count
    #Only calcuate this once
    if _brown_corpus_count is None:
        print("Loading and processing Brown corpus")
        corpus = defaultdict(lambda:0)
        for sentence in brown.sents():
            for word in sentence:
                corpus[word.lower()] += 1
        _brown_corpus_count = corpus # In case we're multithreaded, share only after the dataset is complete
    return _brown_corpus_count

_stop_words_set = None

def get_stop_words_set():
    global _stop_words_set
    #Only calcuate this once. Set global once it is fully constructed
    if _stop_words_set is None:
        s = set(stopwords.words('english'))
        for word in 'would said could us ok'.split(' '):
            s.add(word)

        _stop_words_set = s
    return _stop_words_set


def filter_common_corpus_words(words_count, scale_factor=300):
    """
    A function that removes the words in phrase dictionary that has a frequency lower than its
    frequency in the Brown corpus. Returns a phrase list after the removals.
    """
    # a word count dictionary for all brown corpus words
    corpus_counts = get_brown_corpus_count()
    corpus_total = sum(corpus_counts.values()) 

    # get the total number of words from phrase list dictionary
    total_word_count = sum(words_count.values())
    
    result = []
    
    # include 'rare' words  i.e. have a higher frequency than expected using the Brown corpus
    for word, count in words_count.items():
        word_freq = count / total_word_count
        corpus_freq = corpus_counts.get(word.lower(), 0) / corpus_total
        if word_freq >= (corpus_freq * scale_factor):
             result.append(word)

    return result

def require_minimum_occurence(transactions, min_support, abort_threshold=5000, maximum_phrase=500):
    """
    A function that extracts the mximal frequent sequential patterns from the raw string
    """

    # generate frequent sequential patterns through PrefixSpan library
    if len(transactions) > abort_threshold: # Return an empty result If N is too large
        return []

    ps = PrefixSpan(transactions)
    pattern_count = ps.frequent(min_support, closed=True)

    # sort the frequent items by their frequency 
    sorted_pattern_count = sorted(pattern_count, key=lambda pattern_count:pattern_count[0], reverse=True)
    all_patterns = [pattern[1] for pattern in sorted_pattern_count if len(pattern[1]) > 1]

    # get stop words
    stop_words = get_stop_words_set()

    # filter pattern of length 1
    frequent_once_phrases = dict()
    for pattern in sorted_pattern_count:
        if len(pattern[1]) == 1:
            frequent_once_phrases.update({pattern[1][0] : pattern[0]})
    
    #print("frequent_once_phrases_length", len(frequent_once_phrases))
    #print("frequent_once_phrases", frequent_once_phrases)
    
    filtered_once_phrase = filter_common_corpus_words(frequent_once_phrases, scale_factor=100)
    
    #print("filtered_once_phrase_length", len(filtered_once_phrase))
    #print("filtered_once_phrase", filtered_once_phrase)
    
    nonstop_filtered_once_phrase = filter_stop_words(filtered_once_phrase)
    
    #print("nonstop_filtered_once_phrase_length", len(nonstop_filtered_once_phrase))
    #print("nonstop_filtered_once_phrase", nonstop_filtered_once_phrase)
    
    #print(list(set(frequent_once_phrases) - set(filtered_once_phrase)))
    
    # remove phrases that contains stop words
    nonstop_patterns = []
    for pattern in all_patterns:
        non_stop = True
        for word in pattern:
            if word.lower() in stop_words:
                non_stop = False
                break    
        if non_stop == True:
            nonstop_patterns.append(pattern)
    
    # format the result frequent pattern
    unique_patterns = [' '.join(pattern) for pattern in nonstop_patterns] # ['A B C']
    unique_patterns += nonstop_filtered_once_phrase
    selected_patterns = unique_patterns[:min(maximum_phrase, len(unique_patterns))]

    return selected_patterns

def to_phrase_hints(raw_phrases):
    try:
        canon_map = {} # i -> I. TODO
        #Step 1; gather all of the data across all scenes. 
        all_phrases = [ ] # [ ['The','cat'], ['A', 'dog'],['A', 'dog'],['A', 'dog'],...]
        all_words = [] # ['The', 'cat', 'A', 'dog'' ,'A' ,'dog'']
        # Unwanted punctuation
        p = re.compile(r"(\.|\?|,|:|;|'" + '|")')
        for phrase in raw_phrases.split('\n'): # e.g. data from scene['phrases']:
            words = p.sub(' ', phrase)
           
            words = [w for w in words.split(' ') if len(w) > 0 ]
           
            # construct canon_map, substitute inflection with its lowercase form during internal processing
            for i in range(len(words)):
                #word_origin = WordNetLemmatizer().lemmatize(words[i].lower(),'v')
                word_origin = words[i].lower()
                if word_origin != words[i]:
                    if word_origin not in canon_map.keys():
                       canon_map.update({word_origin : Counter()})
                    canon_map[word_origin][words[i]] += 1 
                    words[i] = word_origin
                else:
                    if word_origin in canon_map.keys():
                        canon_map[word_origin][words[i]] += 1 

            all_phrases.append(words)
            all_words.extend(words)

        #print('all_phrases',all_phrases)
        #print('all_words',all_words)

        words_count = dict( Counter(all_words) ) 
        print('canon_map',canon_map)
        
        delete_inplace_unwanted_characters(words_count)

        words_list = filter_common_corpus_words(words_count) # e.g. dog, cat,

        words_list = filter_stop_words(words_list) # e.g. a, an,the,...

        #  if it occurs fewer times than this, then discard it
        minimum_occurence = 2 
        frequent_phrases= require_minimum_occurence(all_phrases, minimum_occurence)        

        #print('words_list',words_list)
        #print('len_frequent_phrases',len(frequent_phrases))
        #print('frequent_phrases',frequent_phrases)
        result = words_list
        result += frequent_phrases
        result = list(set(result))

        # substitute word with its most common inflection when outputing the result
        for i in range(len(result)):
            splitted_phrase = result[i].split(' ')
            for j in range(len(splitted_phrase)):
                #word_origin = WordNetLemmatizer().lemmatize(splitted_phrase[j].lower(),'v')
                word_origin = splitted_phrase[j].lower()
                if word_origin in canon_map.keys():
                    splitted_phrase[j] = canon_map[word_origin].most_common()[0][0]
            result[i] = ' '.join(splitted_phrase)

        # Remove all single character phrase
        result = [phrase for phrase in result if len(phrase) > 1]
        
        print('final_length',len(result))
        print('result',result)

        return '\n'.join(result)
    
    except Exception as e:
            print("to_phrase_suggestions() throwing Exception:" + str(e))
            raise e
