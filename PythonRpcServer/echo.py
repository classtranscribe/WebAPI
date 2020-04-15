import requests
from utils import encode, decode, getRandomString, download_file
import os
import json

def get_syllabus(publicUrl, stream = 0):
    request1 = requests.get(publicUrl, allow_redirects=False)

    if request1.status_code == 404 or request1.status_code == 500:
        raise Exception("Invalid publicUrl", request1.content)

    location = request1.headers['Location']
    # location is a string of format - /section/6c24cea4-a521-4bf1-9e26-9c91f03ed1b8/home 
    # The uuid here is the sectionId
    sectionId = location[location.find("/",1) + 1:location.rfind("/")]
    request2 = requests.get('https://echo360.org/section/' + sectionId + '/syllabus', allow_redirects=False, cookies = request1.cookies)
    if request2.status_code != 200:
        raise Exception("Invalid publicUrl", request1.content)

    request2_body = request2.json()
    status = request2_body['status']
    syllabus = request2_body['data']
    medias = []

    for obj in syllabus:
        try:
            
            published = obj['lesson']['video']['published']        
            termName = published.get('termName', '')
            lessonName = published.get('lessonName', '')
            courseName = published.get('courseName', '')
            sectionId = published.get('sectionId', '')
            
            media = obj['lesson']['video']['media']        
            current = media['media']['current']
            
            echoMediaId = media['id']
            userId = media['userId']
            institutionId = media['institutionId']        
            createdAt = media['createdAt']
            audioUrl = current['audioFiles'][0]['s3Url']
            primaryFiles = current['primaryFiles']        
            
            secondaryFiles = current.get('secondaryFiles')
            if not secondaryFiles or len(secondaryFiles) == 0:
                # secondary empty or does not exist
                videoUrl = primaryFiles[1]['s3Url']
                altVideoUrl = None
                
            else:
                if stream == 0: 
                    videoUrl = primaryFiles[1]['s3Url'] # 0 for SD, 1 for HD                
                    altVideoUrl = secondaryFiles[1]['s3Url'] # 0 for SD, 1 for HD
                else:
                    videoUrl = secondaryFiles[1]['s3Url'] # 0 for SD, 1 for HD
                    altVideoUrl = primaryFiles[1]['s3Url'] # 0 for SD, 1 for HD
                
                
            mediaJson = {
                    "sectionId": sectionId,
                    "mediaId": echoMediaId,
                    "userId": userId,
                    "institutionId": institutionId,
                    "createdAt": createdAt,
                    "audioUrl": audioUrl,
                    "videoUrl": videoUrl,
                    "altVideoUrl": altVideoUrl,
                    "termName": termName,
                    "lessonName": lessonName,
                    "courseName": courseName
            }
            medias.append(mediaJson)
            
        except Exception as e:
            print('Exception', str(e))
    
    return json.dumps({"medias": medias, "downloadHeader": encode(request1.cookies)}) 

def downloadLecture(url, cookies):
    filePath, extension = download_file(url, cookies = decode(cookies))
    return filePath, extension
