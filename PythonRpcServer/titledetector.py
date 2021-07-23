import cv2
import os
import pytesseract
import numpy as np
from PIL import Image
from collections import Counter
from matplotlib import pyplot as plt

def min_max_normalize(data, inverse=False):
    '''
    Apply min max normalization to a list of float
    
    Parameters:
    data (list of float): Float list to be normalized
    inverse (boolean): If inverse is True, then the original maximum will be normalized to 0, 1 otherwise
    
    Returns:
    list of float: Normalized float list
    '''

    if np.max(data) - np.min(data) == 0:
        return list(np.array(data) / np.min(data))
    
    if inverse == False:
        return list((np.array(data) - np.min(data)) / (np.max(data) - np.min(data)))
    else:
        return list( 1 - (np.array(data) - np.min(data)) / (np.max(data) - np.min(data)) )

def scale_by_text_height(text, original_height, scale_factor=1.10):
    """
    Scale the height of a string based on its height 
    
    Parameters:
    text (string): Text based on which to scale the height
    original_height (float): Original height
    scale_factor (float): Scale factor
    
    Returns:
    float: Scaled height, which should not be larger than original height
    """
    
    upper_factor = ['b', 'd', 'f', 'h', 'i', 'j', 'k', 'l', 't', '?', '!']
    lower_factor = ['g', 'j', 'p', 'q', 'y']
    scaled_height = original_height
    if text.lower() != text:
        scaled_height /= scale_factor
    else:
        for c in upper_factor:
            if c in text:
                scaled_height /= scale_factor
                break
                
    for c in lower_factor:
            if c in text:
                scaled_height /= scale_factor
                break 
                
    return scaled_height

def find_canadiate_in_range(words, xs, ys, heights, x_low, x_high, h_low, h_high):
    '''
    Find possible title word in a specific X range
    
    Parameters:
    words (list of string): Canadiate title word list
    xs (list of string): List of X (vertical) position of possible title word 
    ys (list of string): List of Y (horizontal) position of possible title word 
    heights (list of float): List of scale height of possible title word 
    x_low (float): Lower bound of the X range
    x_high (float): Upper bound of the X range
    h_low (float): Maximum height a potential canadiate word can have
    h_high (float): Minimum height a potential canadiate word can have
    
    Returns:
    list of tuple (string, string): Possible title word in a specific X range with their Y position
    '''
    
    output = []
    for i in range(len(words)):
        word_x = float(xs[i])
        word_h = float(heights[i])
        if word_x >= x_low and word_x < x_high and word_h > h_low and word_h < h_high:
            output.append((words[i], ys[i])) 
    return output

def title_detection(img, ocr_confidence = 70, searching_range = 0.6, same_line_factor = 0.42):
    '''
    Given a PowerPoint Slide as an Image, find the most possible title
    
    Parameters:
    img: image file
    ocr_confidence (int): Minimum OCR confidence for a word to become a title candidate, default is 70
    searching_range (float): Maximum range to search for title candidates on other rows, default is 60% 
    same_line_factor (float): Maximum variation of height for words acorss the same row, default is 42%
    
    
    Returns:
    string: Most possible title for the slide
    '''

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    
    # OCR with text content and bounding box
    text_data = pytesseract.image_to_data(gray, output_type='dict')

    # Basic information about the frame
    height, width, channels = img.shape
    hortizonal_boundary = width / 2.0
    
    # Lists to store OCR outputs
    words = []
    height = []
    x_position = []
    y_position = []
    candidate_score = []
    
    # Extract OCR outputs
    for i in range(len(text_data['conf'])):
        if int(text_data['conf'][i]) >= ocr_confidence and len(text_data['text'][i].strip()) > 0:
            scaled_height = scale_by_text_height(text=text_data['text'][i], original_height=text_data['height'][i])
            
            words.append(text_data['text'][i])
            height.append(scaled_height)
            x_position.append(text_data['top'][i] + 0.5*text_data['height'][i])
            y_position.append(text_data['left'][i] + 0.5*text_data['width'][i])
    
    # OCR did not find enough evdience, return an empty string
    if len(words) == 0:
        return ''
    
    # Data normalization
    height_score = min_max_normalize(height)
    x_score = min_max_normalize(x_position, inverse=True)
    y_score = min_max_normalize(np.abs(np.array(y_position) - hortizonal_boundary), inverse=True)
    
    # Score generation
    for i in range(len(words)):
        word_score = 0.4*height_score[i] + 0.5*x_score[i] + 0.1*y_score[i]
        candidate_score.append(word_score)
    
    candidate_idx = np.argmax(np.array(candidate_score))
    candidate = words[candidate_idx]
    
    # Features of the canadiate
    canadiate_h = height[candidate_idx]
    canadiate_x = x_position[candidate_idx]
    canadiate_y = y_position[candidate_idx]
    
    # Bound for checking other canadiates
    min_height = canadiate_h * (1 - same_line_factor)
    max_height = canadiate_h * (1 + same_line_factor)
    
    top_higher_line = canadiate_x - 5.0 * searching_range * canadiate_h #-2.5
    top_lower_line = canadiate_x - 3.0 * searching_range * canadiate_h #-1.
    
    middle_higher_line = canadiate_x - 1.0 * searching_range * canadiate_h #-0.5
    middle_lower_line = canadiate_x + 1.0 * searching_range * canadiate_h #0.5
    
    bottom_higher_line = canadiate_x + 3.0 * searching_range * canadiate_h #1.5
    bottom_lower_line = canadiate_x + 5.0 * searching_range * canadiate_h #2.5
    
    # Find other words around the canadiate
    up_words = find_canadiate_in_range(words, x_position, y_position, height, top_lower_line, middle_higher_line, min_height, max_height)
    canadiate_words = find_canadiate_in_range(words, x_position, y_position, height, middle_higher_line, middle_lower_line, min_height, max_height)
    low_words = find_canadiate_in_range(words, x_position, y_position, height, middle_lower_line, bottom_higher_line, min_height, max_height)
    
    # Sort the words in the canadiate sequence
    canadiate_sequence = sorted(canadiate_words, key=lambda x: x[1])
    up_sequence = sorted(up_words, key=lambda x: x[1])
    low_sequence = sorted(low_words, key=lambda x: x[1])
    
    # Search for ignored canadiate line
    upper_sequence = []
    if len(up_words) > 0 and len(low_words) == 0:
        upper_words = find_canadiate_in_range(words, x_position, y_position, height, top_higher_line, top_lower_line, min_height, max_height)
        upper_sequence = sorted(upper_words, key=lambda x: x[1])
    
    lower_sequence = []
    if len(low_words) > 0 and len(up_words) == 0:
        lower_words = find_canadiate_in_range(words, x_position, y_position, height, bottom_higher_line, bottom_lower_line, min_height, max_height)
        lower_sequence = sorted(lower_words, key=lambda x: x[1])
    
    # Generate title
    title =  ' '.join([word[0] for word in canadiate_sequence]) 
    
    if len(up_sequence) > 0:
        title = ' '.join([word[0] for word in up_sequence]) + ' ' + title
        if len(upper_sequence) > 0:
            title = ' '.join([word[0] for word in upper_sequence]) + ' ' + title
    
    if len(low_sequence) > 0:
        title = title + ' ' + ' '.join([word[0] for word in low_sequence])    
        if len(lower_sequence) > 0:
            title = title + ' ' + ' '.join([word[0] for word in lower_sequence]) 
    
    return title