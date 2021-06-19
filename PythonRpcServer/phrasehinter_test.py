import nltk
from nltk.probability import FreqDist
from nltk.corpus import brown
from collections import Counter
from nltk.corpus import stopwords
import operator
from string import ascii_letters, digits

import phrasehinter as ph

def test_delete_inplace_unwanted_characters():
	d1 = {'*':5,'apple':1,'..':1,'\n':2,'Fred':2,'N.ope':3,'N:\xF0\x9F\x98\x81,pe':4}
	expected1 ={'apple':1,'Fred':2}

	ph.delete_inplace_unwanted_characters(d1)
	assert(d1 == expected1)

def test_filter_stop_words():
	input = 'i,me,My,myself,table,TABLE,Table,we,our,ours,ourselves,YOU,your,yours,he,him,his,himself,she,her,hers,herself'.split(',')
	result = ph.filter_stop_words(input)
	#print(result)
	assert(result == ['table','TABLE','Table'])

def test_filter_common_corpus_words():
	d1={'face':2,'run':1,'get':2,'randomize':20}
	expected = 'randomize'
	result = ph.filter_common_corpus_words(d1)
	print('Todo: test_filter_common_corpus_words. test input where not all words are returned')
	#print(result)

	#assert(expected == result)

def test_require_minimum_occurence():
	print('test_require_minimum_occurence')
	phrases='Hello\nHow are you?\nWhat will it make of this speech?\nI wonder\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'.split('\n')
	input=[ phrase.split(' ') for phrase in phrases ]
	expected = ['Drinkably Deliciousy Delightful']
	min_support = 2
	result = ph.require_minimum_occurence(input, min_support)
	print('test_require_minimum_occurence:',result)
	assert(result == expected)
	pass

def test_to_phrase_hints():
	input = 'Hello\nHow are you?\nWhat will it, make of this speech?\nI wonder;...\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'
	result = ph.to_phrase_hints(input)
	print('test_to_phrase_hints:',result)

def run_phrasehinter_tests():
	test_delete_inplace_unwanted_characters()
	test_filter_stop_words()
	test_filter_common_corpus_words()
	test_require_minimum_occurence()
	test_to_phrase_hints()

if __name__ == '__main__': 
	run_phrasehinter_tests();
	print('done');
