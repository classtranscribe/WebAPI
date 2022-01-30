import numpy as np
import pytesseract
import cv2

def get_test_image():
    font = cv2.FONT_HERSHEY_SIMPLEX
    org = (50,300)
    fontScale = 1
    color = (255,255,255)
    thickness = 2
    text = 'Test'
    width,height = (1024,720)
    image = np.zeros((height,width,3), np.uint8)
    image = cv2.putText(image,text,org,font,fontScale,color,thickness,cv2.LINE_AA)
    return image

def tesseract_test():
    frame = get_test_image()
    gray_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    ocr_result = pytesseract.image_to_data(gray_frame, output_type='dict')
    print(ocr_result)
    assert( 'Test' == ocr_result['text'][0])

if __name__ == '__main__' :
    tesseract_test()