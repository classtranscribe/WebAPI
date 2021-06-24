import time
import nltk
import phrasehinter as ph

from string import ascii_letters, digits
from collections import Counter

_440_all_phrases = None
_440_words_counter = None

def get_440_lecture_transcription():
	global _440_all_phrases, _440_words_counter
	
	if _440_all_phrases == None and _440_words_counter == None:
		f = open('cs440_transcription.txt', 'r')
		raw_lines = f.readlines()
		lines = list(filter(('\n').__ne__, raw_lines))

		_440_words_counter = Counter()
		_440_all_phrases = []
		for line in lines:
			transcation = []
			for word in line.split(' '):
				if word[len(word)-1] == '\n':
					if len(set(word[:len(word)-1]).difference(set(ascii_letters))) == 0:
						_440_words_counter[word[:len(word)-1]] += 1
						transcation.append(word[:len(word)-1])
				else:
					if len(set(word[:len(word)-1]).difference(set(ascii_letters))) == 0:
						_440_words_counter[word] += 1
						transcation.append(word)
			if len(transcation) > 0:
				_440_all_phrases.append(transcation)

	return _440_all_phrases, _440_words_counter

def test_delete_inplace_unwanted_characters():
	print("----------test_delete_inplace_unwanted_characters---STARTED----------")
	d1 = {'*':5,'apple':1,'..':1,'\n':2,'Fred':2,'N.ope':3,'N:\xF0\x9F\x98\x81,pe':4}
	expected1 ={'apple':1,'Fred':2}
	ph.delete_inplace_unwanted_characters(d1)

	assert(d1 == expected1)
	print("----------test_delete_inplace_unwanted_characters---PASSED----------")

def test_filter_stop_words():
	print("----------test_filter_stop_words---STARTED----------")
	input = 'i,me,My,myself,table,TABLE,Table,we,our,ours,ourselves,YOU,your,yours,he,him,his,himself,she,her,hers,herself'.split(',')
	result = ph.filter_stop_words(input)

	assert(result == ['table','TABLE','Table'])
	print("----------test_filter_stop_words---PASSED----------")

def test_filter_common_corpus_words():
	print("----------test_filter_common_corpus_words---STARTED----------")

	# Test 1: Testing General Input
	d1={'face':2,'run':1,'get':2,'randomize':20}
	expected1 = ['randomize']
	result1 = ph.filter_common_corpus_words(d1)

	# Test 2: Testing with CS 440 Lecture
	_440_all_phrases, _440_words_counter = get_440_lecture_transcription()
	expected_filtered_2 = ['be', 'and', 'of', 'a', 'in', 'to', 'have', 'too', 'it', 'I']
	expected_perserved_2 = ['algorithm', 'breadth', 'computational']
	result2 = ph.filter_common_corpus_words(_440_words_counter)

	for phrase in expected_filtered_2:
		assert(phrase not in result2)
	
	for phrase in expected_perserved_2:
		assert(phrase in result2)

	assert(expected1 == result1)
	print("----------test_filter_common_corpus_words---PASSED----------")
	

def test_require_minimum_occurence():
	print("----------test_require_minimum_occurence---STARTED----------")
	min_support = 2

	# Test 1: Testing General Input
	phrases_1='Hello\nHow are you?\nWhat will it make of this speech?\nI wonder\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'.split('\n')
	input_1=[ phrase.split(' ') for phrase in phrases_1 ]
	expected_1 = ['Drinkably Deliciousy Delightful']
	result_1 = ph.require_minimum_occurence(input_1, min_support)
	assert(result_1 == expected_1)

	# Test 2: Testing very large Input
	num_sentences = 10000 
	input_2 = nltk.corpus.brown.sents()[:num_sentences] 
	
	expected_2 = []
	result_2 = ph.require_minimum_occurence(input_2, min_support)
	assert(result_2 == expected_2)

	# Test 3: Testing For Minimum Support
	phrases_3='Hello\nThe weather is so nice\nThe weather is so nice\nThe weather is so nice\nThe weather is so nice\ngenerative adversarial networks\ngenerative adversarial networks\ngenerative adversarial networks\nI wonder\nI wonder\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'.split('\n')
	input_3=[ phrase.split(' ') for phrase in phrases_3 ]
	expected_3 = ['Drinkably Deliciousy Delightful', 'generative adversarial networks']
	result_3 = ph.require_minimum_occurence(input_3, 3)
	assert(len(result_3) == len(expected_3))
	for phrase in expected_3:
		assert(phrase in result_3)
	
	# Test 4: Testing Closure Property
	phrases_4='lion kiss\nkiss dear\nkiss dear\nlion kiss dear\nlion kiss\nlion kiss dear Drinkably Deliciousy Delightful\nlion kiss dear'.split('\n')
	input_4=[ phrase.split(' ') for phrase in phrases_4 ]
	expected_4 = ['lion kiss', 'kiss dear', 'lion kiss dear']
	result_4 = ph.require_minimum_occurence(input_4, min_support)
	for phrase in expected_4:
		assert(phrase in result_4)

	'''
	# Test 5: Testing Maximum Phrase
	num_sentences = 50
	input_5 = nltk.corpus.brown.sents()[:num_sentences] # List of List of words
	result_5 = ph.require_minimum_occurence(input_5, min_support)
	assert(len(result_5) == 500
	'''

	# Test 6: Testing With CS440 Lecture
	_440_all_phrases, _440_words_counter = get_440_lecture_transcription()

	expected_6 = ['depth first search', 'breadth first search', 'time complexity', 'space complexity']
	result_6 = ph.require_minimum_occurence(_440_all_phrases, min_support)
	for phrase in expected_6:
		assert(phrase in result_6)

	print("----------test_require_minimum_occurence---PASSED----------")

def test_to_phrase_hints():
	#input = 'Hello\nHow are you?\nWhat will it, make of this speech?\nI wonder;...\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'
	#result = ph.to_phrase_hints(input)
	#print('test_to_phrase_hints:',result)

	print("----------test_to_phrase_hints---STARTED----------")
	
	# Test 1: Testing With CS440 Lecture
	_440_all_phrases, _440_words_counter = get_440_lecture_transcription()

	text = '\n'.join( [' '.join(words) for words in _440_all_phrases] )
	text = text.replace('``','')
	print(f"{len(text)} characters")
	start_time = time.time()
	result = ph.to_phrase_hints(text)
	duration = time.time()- start_time
	print(f"{duration:.2} seconds.")
	
	'''
	# Test 2: Testing Canon Map
	all_phrases = [['Einstein', 'Einstein', 'Einstein'], ['einstein', 'einstein'], ['EINSTEIN']]

	text2 = '\n'.join( [' '.join(words) for words in all_phrases] )
	text2 = text2.replace('``','')
	print(f"{len(text2)} characters")
	start_time2 = time.time()
	result2 = ph.to_phrase_hints(text2)
	duration = time.time()- start_time2
	print(f"{duration:.2} seconds.")
	'''

	print("----------test_to_phrase_hints---PASSED----------")


def test_corpus_long_input():
	print("----------test_corpus_long_input---STARTED----------")
	num_sentences = 100
	corpus = nltk.corpus.brown.sents()[:num_sentences] # List of List of words
	
	text = '\n'.join( [' '.join(words) for words in corpus] )
	text = text.replace('``','')
	print(f"{len(text)} characters")
	start_time = time.time()
	result = ph.to_phrase_hints(text)
	duration = time.time()- start_time
	print(f"{duration:.2} seconds.")

	print("----------test_corpus_long_input---PASSED----------")

def test_corpus_data():
	print("----------test_corpus_data---STARTED----------")
	stop_words = ph.get_stop_words_set()
	assert('said' in stop_words)
	t1 = time.time()
	brown = ph.get_brown_corpus_count()
	print(f"Brown corpus loading time {time.time() - t1:.2} seconds")
	assert(brown.get('the') > 1000)
	assert(brown.get('if') > brown.get('brother'))
	assert('spacejamindex' not in brown)
	t = time.time()
	brown2 = ph.get_brown_corpus_count()
	stop_words2 = ph.get_stop_words_set()
	is_cached = (time.time() - t) < 0.25
	assert(is_cached)
	print("----------test_corpus_data---PASSED----------")



def run_phrasehinter_tests():
	test_corpus_data()
	test_delete_inplace_unwanted_characters()
	test_filter_stop_words()
	test_filter_common_corpus_words()
	test_require_minimum_occurence()
	test_to_phrase_hints()
	test_corpus_long_input()

if __name__ == '__main__': 
	run_phrasehinter_tests();
	print('done');
