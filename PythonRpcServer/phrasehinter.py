import re
import operator

from string import ascii_letters, digits
from collections import Counter,defaultdict
from nltk.corpus import brown,stopwords



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

    s = set(stopwords.words('english'))
    output = [ w for w in phraseList if w.lower() not in s]

    return output

_brown_corpus_count = None

def get_brown_corpus_count():
    global _brown_corpus_count
    #Only calcuate this once
    if _brown_corpus_count is None:
        corpus = defaultdict(lambda:0)
        for sentence in brown.sents():
            for word in sentence:
                corpus[word.lower()] += 1
        _brown_corpus_count = corpus # In case we're multithreaded, share only after the dataset is complete
    return _brown_corpus_count


def filter_common_corpus_words(words_count):
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
        if word_freq >= corpus_freq :
             result.append(word)

    return result

def require_minimum_occurence(transactions, min_support):
    """
    A function that extracts the frequent itemsets from the phrase lists from OCR
    """

    def make_n(last, second, n, makedict):
        #unusedlist_last = list(last)
        output = []
        for name in last:
            for second_name in second:
                if name[n - 1] == second_name[0]:
                    temp_list = list(name)
                    tempcpy = temp_list.copy()
                    temp_list.append(second_name[1])
                    output.append(tuple(temp_list))
                    t = frozenset(temp_list)
                    makedict[t] = tempcpy
        return output

    def n_check(trans, names, n):
        n_list = {}
        removelist = []
        subset = []
        for name in names:
            counter = 0
            for sentence in trans:
                for i in range(len(sentence) - n + 1):
                    has = True
                    for j in range(n):
                        if sentence[i + j] != name[j]:
                            has = False
                    if (has):
                        counter += 1
            if (counter >= 2):
                fname = frozenset(name)
                son = makedict[fname]
                son = [son]
                for i in range(len(name) - 1):
                    if i == 0:
                        continue
                    subset = subset + [name[i:len(name)]]
                removelist = removelist + son
                n_list[name] = counter
        return n_list, removelist, subset

    double_base_counter = Counter()
    makedict = {}
    for sentence in transactions:
        for i in range(len(sentence) - 1):
            temp = (sentence[i], sentence[i + 1])
            double_base_counter[temp] += 1

    base_list2 = dict((word_pair, double_base_counter[word_pair]) for word_pair in double_base_counter if
                      double_base_counter[word_pair] >= min_support)
    second_support_names = list(base_list2)

    last = base_list2
    count = 2
    output = base_list2.copy()
    while (len(last) > 0 and count < 5):
        names_new = make_n(last, second_support_names, count, makedict)
        last, removelist, subs = n_check(transactions, names_new, count + 1)
        output.update(last)
        for i in removelist:
            if tuple(i) in output.keys():
                del output[tuple(i)]
        for i in subs:
            if i in output:
                del output[i]
        count += 1

    sorted_a = sorted(output.items())
    sorted_value = sorted(sorted_a, key=operator.itemgetter(1), reverse=True)

    result = [' '.join(words) for words,count in sorted_value]
    return result


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

           all_phrases.append(words)
           all_words.extend(phrase.split(' '))

        #print('all_phrases',all_phrases)
        #print('all_words',all_words)

        words_count = dict( Counter(all_words) ) 
        
        delete_inplace_unwanted_characters(words_count)

        words_list = filter_common_corpus_words(words_count) # e.g. dog, cat,

        words_list = filter_stop_words(words_list) # e.g. a, an,the,...

         #  if it occurs fewer times than this, then discard it
        minimum_occurence = 2 
        frequent_phrases= require_minimum_occurence(all_phrases, minimum_occurence)
        print('words_list',words_list)
        print('frequent_phrases',frequent_phrases)
        result = words_list
        result += frequent_phrases
        
        return '\n'.join(result)
    
    except Exception as e:
            print("to_phrase_suggestions() throwing Exception:" + str(e))
            raise e
