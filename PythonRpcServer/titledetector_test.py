import titledetector as td
import pytesseract
import cv2
import urllib.request
import os

# General Testing Scheme For All Test Cases
def test_scheme(image_name, url, expected_result):
    # Download the sample slide from Box
    urllib.request.urlretrieve(url, image_name)
    
    # Change the sample slide into gray scale
    img = cv2.imread(image_name, cv2.IMREAD_COLOR)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    
    # Perform the OCR for the sample slide
    str_text = pytesseract.image_to_data(gray, output_type='dict')
    
    # Run TitleDetector upon the pytesseract result
    frame_height, frame_width, frame_channels = img.shape
    title = td.title_detection(str_text, frame_height, frame_width)
    
    # Delete the downloaded image file
    os.remove(image_name)
    
    assert(title == expected_result)
    print('Result for '  + image_name + ' is correct!' )

def run_titledetector_tests():
    image_names = [
    'two_line_test.jpeg',
    'chem_102_test.jpeg',
    'three_line_test.jpeg',
    'words_limit_test.jpeg',
    'untitled_test.jpeg',
    'cs_418_test.jpeg',
    'text_in_background_test.jpeg',
    'middle_right_title_test.jpeg',
    'text_with_number_test.jpeg',
    'middle_title_test.jpeg'
    ]

    urls = [
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=1phs3sikitxdw0vjaryiu6isu3oq5yii&file_id=f_839325860753',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=jmac3vq3n7p1fg71suhwa1h1xadhual4&file_id=f_839327923696',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=41271vrfzgzrtqjnsktb2fm7t9xdm68t&file_id=f_839327603328',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=00abfapnqimrmk3q6l5sliqdgn3gaug1&file_id=f_839327514553',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=v01hqc4jb3eghh3ht88lita0zpk72vka&file_id=f_839325009914',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=l8e5yhz0vxl7u1g3mfyte4y8vye4sfpa&file_id=f_839324975094',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=xnrfm1aoei3f61k3jq8w7l48s2w1reax&file_id=f_839327396585',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=41f4jba1xn3dlokxaaz8aijw4e76700u&file_id=f_839326174324',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=46akcl4psp9j5szsgn0ch5h14q31wcki&file_id=f_839324672130',
    'https://app.box.com/index.php?rm=box_download_shared_file&shared_name=o3osn2appwflfqqz6k9lq1f31337bhmz&file_id=f_839326678905'
    ]

    expected_results = [
    'BUILDING A BETTER VIDEO PLAYER',
    '4.1 IONIC BONDING (CONT. FROM CH. 03)',
    '2020 ASEE: How Introduction of ClassTranscribe is Changing Engineering Education at the University of Illinois',
    'Mining extra slide from the so called thing which is in Quantitative Association kill Predicate Sets Mining extraordinary Redundancy Filtering',
    '',
    'Gamma Correction in sRGB',
    'Data Visualization and Storytelling',
    '2019 ClassTranscribe...',
    'Nav1.1 Mutations and Epilepsy',
    'Title Detection For Videos'
    ]

    for i in range(len(urls)):
        test_scheme(image_names[i], urls[i], expected_results[i])

if __name__ == '__main__': 
    run_titledetector_tests();
    print('done');