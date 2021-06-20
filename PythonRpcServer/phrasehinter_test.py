import time
import nltk
import phrasehinter as ph

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
	d1={'face':2,'run':1,'get':2,'randomize':20}
	expected = ['randomize']
	result = ph.filter_common_corpus_words(d1)

	assert(expected == result)
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
	phrases_3='Hello\nThe weather is so nice\nThe weather is so nice\nThe weather is so nice\nThe weather is so nice\nHow are you?\nHow are you?\nHow are you?\nI wonder\nI wonder\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'.split('\n')
	input_3=[ phrase.split(' ') for phrase in phrases_3 ]
	expected_3 = ['Drinkably Deliciousy Delightful', 'How are you?', 'The weather is so nice']
	result_3 = ph.require_minimum_occurence(input_3, 3)
	assert(len(result_3) == len(expected_3))
	for phrase in expected_3:
		assert(phrase in result_3)
	
	# Test 4: Testing Closure Property
	phrases_4='I wonder\nwonder it\nwonder it\nI wonder it\nI wonder\nI wonder it Drinkably Deliciousy Delightful\nI wonder it'.split('\n')
	input_4=[ phrase.split(' ') for phrase in phrases_4 ]
	expected_4 = ['I wonder it', 'wonder it', 'I wonder']
	result_4 = ph.require_minimum_occurence(input_4, min_support)
	assert(len(result_4) == len(expected_4))
	for phrase in expected_4:
		assert(phrase in result_4)

	print("----------test_require_minimum_occurence---PASSED----------")

def test_to_phrase_hints():
	input = 'Hello\nHow are you?\nWhat will it, make of this speech?\nI wonder;...\nDrinkably Deliciousy Delightful\nDrinkably Deliciousy Delightful'
	result = ph.to_phrase_hints(input)
	print('test_to_phrase_hints:',result)

def test_corpus_long_input():
	print("----------test_corpus_long_input---STARTED----------")
	num_sentences = 500
	corpus = nltk.corpus.brown.sents()[:num_sentences] # List of List of words
	
	text = '\n'.join( [' '.join(words) for words in corpus] )
	text = text.replace('``','')
	print(f"{len(text)} characters")
	start_time = time.time()
	result = ph.to_phrase_hints(text)
	duration = time.time()- start_time
	print(f"{duration:.2} seconds.")

	print("----------test_corpus_long_input---PASSED----------")


def run_phrasehinter_tests():
	test_delete_inplace_unwanted_characters()
	test_filter_stop_words()
	test_filter_common_corpus_words()
	test_require_minimum_occurence()
	#test_to_phrase_hints()
	test_corpus_long_input()

if __name__ == '__main__': 
	run_phrasehinter_tests();
	print('done');
