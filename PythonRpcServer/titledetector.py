import cv2
import numpy as np

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

def scale_by_text_height(text, original_height):
    """
    Scale the height of a string based on its height 
    
    Parameters:
    text (string): Text based on which to scale the height
    original_height (float): Original height
    
    Returns:
    float: Scaled height, which should not be larger than original height
    """

    SCALE_FACTOR = 1.10 # Negative (decreasing) scaling multiplier
    
    upper_factor = ['b', 'd', 'f', 'h', 'i', 'j', 'k', 'l', 't', '?', '!']
    lower_factor = ['g', 'j', 'p', 'q', 'y']
    scaled_height = original_height
    if text.lower() != text:
        scaled_height /= SCALE_FACTOR
    else:
        for c in upper_factor:
            if c in text:
                scaled_height /= SCALE_FACTOR
                break
                
    for c in lower_factor:
            if c in text:
                scaled_height /= SCALE_FACTOR
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
    list of tuple (string, string): Possible title word in a specific X range 
    with their Y position
    '''
    
    output = []
    for i in range(len(words)):
        word_x = float(xs[i])
        word_h = float(heights[i])
        if word_x >= x_low and word_x < x_high and word_h > h_low and word_h < h_high:
            output.append((words[i], ys[i])) 
    sequence = sorted(output, key=lambda x: x[1])
    
    return [word[0] for word in sequence]

def generate_boundary(canadiate_x, canadiate_h):
    '''
    Generate horizontal searching boundries based on the candidate and range factor
    
    Parameters:
    canadiate_x (float): X position of the canadiate
    canadiate_h (float): Scaled height of the canadiate
    
    Returns:
    list of float: Searching boundries
    '''

    # CONSTANTS
    SEARCH_RANGE = 0.6 # Search range multiplier for title candidates on other rows 
    
    output = np.array([-5., -3., -1., 1., 3., 5.])
    return canadiate_x + output * SEARCH_RANGE * canadiate_h

def title_detection(text_data, height, width):
    '''
    Given the height, width, abd pytesseract dict output of an image, find the most possible title
    
    Parameters:
    text_data (dict): Pytesseract output
    height (int): Image height
    width (int): Image width
    
    Returns:
    string: Most possible title for the slide
    '''

    # CONSTANTS
    MAXIMUM_NUM_WORDS = 20 # Maximum number of words in a title
    SAME_LINE_FACTOR = 0.42 # Maximum variation of height for words acorss the same row
    OCR_CONFIDENCE = 70 # Minimum OCR confidence for a word to become a title candidate
    
    
    # Basic information about the frame
    hortizonal_boundary = width / 2.0
    
    # Lists to store OCR outputs
    words = []
    height = []
    x_position = []
    y_position = []
    candidate_score = []
    
    # Extract OCR outputs
    for i in range(len(text_data['conf'])):
        if int(text_data['conf'][i]) >= OCR_CONFIDENCE and len(text_data['text'][i].strip()) > 0:
            scaled_height = scale_by_text_height(text=text_data['text'][i], original_height=text_data['height'][i])
            
            words.append(text_data['text'][i])
            height.append(scaled_height)
            x_position.append(text_data['top'][i] + 0.5*text_data['height'][i])
            y_position.append(text_data['left'][i] + 0.5*text_data['width'][i])
    
    # OCR did not find enough evdience, return an empty string
    if len(words) == 0:
        return ''
    
    # Normalize all features to a value between 0 and 1
    height_score = min_max_normalize(height)
    x_score = min_max_normalize(x_position, inverse=True)
    y_score = min_max_normalize(np.abs(np.array(y_position) - hortizonal_boundary), inverse=True)
    
    # Generate score (Probability of a word being the candidate)
    for i in range(len(words)):
        word_score = 0.4*height_score[i] + 0.5*x_score[i] + 0.1*y_score[i]
        candidate_score.append(word_score)
    
    # Find the index, height and X position of the candidate
    candidate_idx = np.argmax(np.array(candidate_score))
    canadiate_h = height[candidate_idx]
    canadiate_x = x_position[candidate_idx]

    # Boundaries for checking height variation of words in the same line
    min_height = canadiate_h * (1 - SAME_LINE_FACTOR)
    max_height = canadiate_h * (1 + SAME_LINE_FACTOR)
    
    # Generate searching boundaries along X axis (5 searching areas are generated)
    boundaries = generate_boundary(canadiate_x, canadiate_h)
    area_available = np.zeros(len(boundaries))
    
    # Searching for potential title words in the middle 3 searching areas
    title_list = []
    for i in range(1, 4):
        line_list = find_canadiate_in_range(words, x_position, y_position, height, boundaries[i], boundaries[i+1], min_height, max_height)
        if len(line_list) > 0:
            title_list += line_list
            area_available[i] = 1.
    
    # If found nothing in the top line, search the bottom further (only when less than 20 title words were detected)
    if area_available[1] == 1. and area_available[3] == 0. and len(title_list) < MAXIMUM_NUM_WORDS:
        title_list = find_canadiate_in_range(words, x_position, y_position, height, boundaries[0], boundaries[1], min_height, max_height) + title_list
    
    # If found nothing in the bottom line, search the top further (only when less than 20 title words were detected)
    if area_available[3] == 1. and area_available[1] == 0. and len(title_list) < MAXIMUM_NUM_WORDS:
        title_list += find_canadiate_in_range(words, x_position, y_position, height, boundaries[4], boundaries[5], min_height, max_height)
    
    # Convert the word list into a string
    title =  ' '.join(title_list) 
    
    return title