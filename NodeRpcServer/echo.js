var rp = require('request-promise');
var path = require('path');
var fs  = require('fs-promise');
const _datadir = process.env.DATA_DIRECTORY;
var utils = require('./utils');

async function requestCookies(publicAccessUrl, playlistId) {
    console.log("requestCookies");
    var cookieFile = playlistId + '.txt';
    const { spawn } = require('child-process-promise');
    const curl = spawn('curl', ['-D', cookieFile, publicAccessUrl]);
    curl.childProcess.stdout.on('data', (data) => {
        console.log(`stdout: ${data}`);
    });

    curl.childProcess.stderr.on('data', (data) => {
        console.log(`stderr: ${data}`);
    });
    await curl;
    return fs.readFile(cookieFile)
        .then(f => {
            var lines = f.toString().split('\n');
            var Cookies = ['PLAY_SESSION', 'CloudFront-Key-Pair-Id', 'CloudFront-Policy', 'CloudFront-Signature'];
            var value_Cookies = ['', '', '', ''];
            for(var i in lines) {
                let line = lines[i];
                for(var j in Cookies) {
                    let index = line.indexOf(Cookies[j]);
                    if(index != -1) {
                        value_Cookies[j] = line.substring(index + Cookies[j].length + 1, line.indexOf(';'));
                        break;
                    }
                }
            }
            var fullText = f.toString();
            var sectionId = fullText.substring(fullText.indexOf('section') + 'section'.length + 1, fullText.indexOf('home') - 1);

            return Promise.resolve({
                PLAY_SESSION: value_Cookies[0],
                cloudFront_Key_Pair_Id: value_Cookies[1],
                cloudFront_Policy: value_Cookies[2],
                cloudFront_Signature: value_Cookies[3],
                sectionId: sectionId
            });
        });
}

async function get_syllabus(cookiesAndHeader) {
    var play_session_login = 'PLAY_SESSION' + "=" + cookiesAndHeader.cookieJson['PLAY_SESSION'];
    var sectionId = cookiesAndHeader.cookieJson.sectionId;
    var options_syllabus = {
        method: 'GET',
        url: 'https://echo360.org/section/' + sectionId + '/syllabus',
        resolveWithFullResponse: true,
        headers:
        {
            'Content-Type': 'application/x-www-form-urlencoded',
            Cookie: play_session_login
        }
    };
    var response_syllabus = await rp(options_syllabus);
    var syllabus = JSON.parse(response_syllabus.body);
    return Promise.resolve(syllabus);
}

async function downloadEchoPlaylistInfo(playlist) {
    var cookiesAndHeader = await requestCookies(playlist.Url, playlist.Id)
        .then(cookieJson => {
            let download_header = 'Cookie: CloudFront-Key-Pair-Id=' + cookieJson.cloudFront_Key_Pair_Id;
            download_header += "; CloudFront-Policy=" + cookieJson.cloudFront_Policy;
            download_header += "; CloudFront-Signature=" + cookieJson.cloudFront_Signature;
            return Promise.resolve({
                cookieJson: cookieJson,
                download_header: download_header
            });
        });
    var syllabus = await get_syllabus(cookiesAndHeader);
    var extractedSyllabus = extractSyllabusAndDownload(syllabus, cookiesAndHeader.download_header, playlist.stream);
    return extractedSyllabus;
  }

  function extractSyllabusAndDownload(syllabus, download_header, stream = 0) {
    console.log("Extracting Syllabus");
    var audio_data_arr = syllabus['data'];
    var medias = [];
    for (var j = 0; j < audio_data_arr.length; j++) {
        var audio_data = audio_data_arr[j];
        try {            
            var media = audio_data['lesson']['video']['media'];
            var sectionId = audio_data['lesson']['video']['published']['sectionId'];
            var echoMediaId = media['id'];
            var userId = media['userId'];
            var institutionId = media['institutionId'];
            var createdAt = media['createdAt'];
            var audioUrl = media['media']['current']['audioFiles'][0]['s3Url'];
            var videoUrl;
            var altVideoUrl;
            if (stream == 0) {
                videoUrl = media['media']['current']['primaryFiles'][1]['s3Url']; // 0 for SD, 1 for HD
                altVideoUrl = media['media']['current']['secondaryFiles'][1]['s3Url']; // 0 for SD, 1 for HD
            } else {
                videoUrl = media['media']['current']['secondaryFiles'][1]['s3Url']; // 0 for SD, 1 for HD
                altVideoUrl = media['media']['current']['primaryFiles'][1]['s3Url']; // 0 for SD, 1 for HD
            }
            var termName = audio_data['lesson']['video']['published']['termName'];
            var lessonName = audio_data['lesson']['video']['published']['lessonName'];
            var courseName = audio_data['lesson']['video']['published']['courseName'];

            
            var mediaJson = {
                sectionId: sectionId,
                mediaId: echoMediaId,
                userId: userId,
                institutionId: institutionId,
                createdAt: new Date(createdAt),
                audioUrl: audioUrl,
                videoUrl: videoUrl,
                altVideoUrl: altVideoUrl,
                download_header: download_header,
                termName: termName,
                lessonName: lessonName,
                courseName: courseName
            };

            medias.push(mediaJson);
        } catch (err) {
            // console.log(err);
        }
    }
    return medias;
}  

async function downloadFile(url, dest, header= "") {
    console.log('downloadFile');
    console.log(url, header, dest);
    const { spawn } = require('child-process-promise');
    header = header.replace(';','\;');
    const curl = spawn("curl", ["-L", "-o", dest, "-O", url, "-H", header, "--silent"]);
    curl.childProcess.stdout.on('data', (data) => {
        console.log(`stdout: ${data}`);
    });

    curl.childProcess.stderr.on('data', (data) => {
        console.log(`stderr: ${data}`);
    });
    await curl;
    return dest;
}

async function downloadEchoLecture(mediaId, videoUrl, download_header) {
    console.log("downloadEchoLecture");
    var dest = _datadir + "/" + mediaId + "_" + utils.getRandomString() + '_'  + videoUrl.substring(videoUrl.lastIndexOf('.'));
    var outputFile = await downloadFile(videoUrl, dest, download_header);
    console.log("Outputfile " + outputFile);
    return outputFile;
}

async function downloadKalturaLecture(mediaId, videoUrl) {
    console.log("downloadKalturaLecture");
    var dest = _datadir + "/" + mediaId + "_" + utils.getRandomString() + '.mp4';
    var outputFile = await downloadFile(videoUrl, dest);
    console.log("Outputfile " + outputFile);
    return outputFile;
}

function getEchoPlaylistRPC(call, callback) {
    console.log(call.request);
    var medias;
    (async () => {
        medias = await downloadEchoPlaylistInfo(call.request);        
        callback(null, {json: JSON.stringify(medias)});
    })();
    
}

function downloadEchoVideoRPC(call, callback) {
    console.log(call.request);
    var outputFile;
    (async () => {
        outputFile = await downloadEchoLecture(call.request.Id, call.request.videoUrl, call.request.additionalInfo);
        callback(null, {filePath: outputFile});
    })();    
}

function downloadKalturaVideoRPC(call, callback) {
    console.log(call.request);
    var outputFile;
    (async () => {
        outputFile = await downloadKalturaLecture(call.request.Id, call.request.videoUrl);
        callback(null, {filePath: outputFile});
    })();    
}

module.exports = {
    getEchoPlaylistRPC: getEchoPlaylistRPC,
    downloadEchoVideoRPC: downloadEchoVideoRPC,
    downloadEchoLecture: downloadEchoLecture,
    downloadKalturaVideoRPC: downloadKalturaVideoRPC
}
