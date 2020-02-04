var rp = require('request-promise');
var path = require('path');
var fs  = require('fs-promise');
var utils = require('./utils');
const _datadir = process.env.DATA_DIRECTORY;
var youtube_google_api_key = process.env.YOUTUBE_API_KEY;

async function downloadYoutubePlaylist(playlist) {
    // TODO: Support more than 50 videos
    var url_playlist = 'https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&' +
        'playlistId=' + playlist.Url + '&key=' + youtube_google_api_key + '&maxResults=' + 50;
    console.log(url_playlist);
    var body = await rp({ url: url_playlist });
    var body_playlist_json = JSON.parse(body);
    var arr_videoInfo = body_playlist_json['items'];
    var medias = []
    arr_videoInfo.forEach(videoInfo => {
        var publishedAt = videoInfo['snippet']['publishedAt'];
        var channelId = videoInfo['snippet']['channelId'];
        var title = videoInfo['snippet']['title']
        var description = videoInfo['snippet']['description'];
        var channelTitle = videoInfo['snippet']['channelTitle'];
        var playlistId = videoInfo['snippet']['playlistId'];
        var videoId = videoInfo['snippet']['resourceId']['videoId'];
        var videoUrl = 'http://www.youtube.com/watch?v=' + videoId;
        var media = {
            channelTitle: channelTitle,
            channelId: channelId,
            playlistId: playlistId,
            title: title,
            description: description,
            publishedAt: publishedAt,
            videoUrl: videoUrl,
            videoId: videoId,
            createdAt: new Date(publishedAt)
        };
        medias.push(media);
    });
    return medias;
}


async function downloadYoutubeVideo(mediaId, videoUrl) {
    var outputFile = _datadir + "/" + mediaId + "_" + utils.getRandomString() + '.mp4';
    const { spawn } = require('child-process-promise');
    const youtubedl = spawn('youtube-dl', [videoUrl, '--format=18', '--output', outputFile]);

    youtubedl.childProcess.stdout.on('data', (data) => {
        console.log(`stdout: ${data}`);
    });

    youtubedl.childProcess.stderr.on('data', (data) => {
        console.log(`stderr: ${data}`);
    });
    await youtubedl;
    return outputFile;
}

function getYoutubePlaylistRPC(call, callback) {
    console.log(call.request);
    (async () => {
        var medias = await downloadYoutubePlaylist(call.request);
        callback(null, { json: JSON.stringify(medias) });
    })();        
}

function downloadYoutubeVideoRPC(call, callback) {
    console.log(call.request);
    (async () => {
        var outputFile = await downloadYoutubeVideo(call.request.Id, call.request.videoUrl);
        console.log("youtube output file: ", outputFile);
        callback(null, { filePath: outputFile });    
    })();    
}

module.exports = {
    getYoutubePlaylistRPC: getYoutubePlaylistRPC,
    downloadYoutubeVideoRPC: downloadYoutubeVideoRPC
}
