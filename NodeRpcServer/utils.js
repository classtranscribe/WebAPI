const _dirname = process.env.DATA_DIRECTORY;

async function asyncForEach(array, callback) {
    for (let index = 0; index < array.length; index++) {
        await callback(array[index], index, array);
    }
}

function convertMStoTime(duration) {
    let milliseconds = parseInt((duration % 1000)),
        seconds = parseInt((duration / 1000) % 60),
        minutes = parseInt((duration / (1000 * 60)) % 60),
        hours = parseInt((duration / (1000 * 60 * 60)) % 24);

    hours = (hours < 10) ? "0" + hours : hours;
    minutes = (minutes < 10) ? "0" + minutes : minutes;
    seconds = (seconds < 10) ? "0" + seconds : seconds;

    return hours + ":" + minutes + ":" + seconds + "." + milliseconds;
}

function stringToDate(dateString) {
    // Require format - yyyy-mm-dd
    var parts = dateString.split('-');
    // Please pay attention to the month (parts[1]); JavaScript counts months from 0:
    // January - 0, February - 1, etc.
    return new Date(parts[0], parts[1] - 1, parts[2]);
}

function runSynchronously(f) {
    (async () => {
        await f();
    })();
}

function getRandomString() {
    return Math.random().toString(36).substring(10);
}

async function convertVideoToWav(pathToFile) {
    console.log("convertVideoToWav");
    var fileName = pathToFile.substring(pathToFile.lastIndexOf('/') + 1, pathToFile.lastIndexOf('.')) + '.wav';
    var outputFile = _dirname + "/" + fileName;
    var tempFile = "/tmp/" + fileName;
    const { spawn } = require('child-process-promise');
    const ffmpeg = spawn('ffmpeg', ['-nostdin', '-i', pathToFile, '-c:a', 'pcm_s16le', '-ac', '1', '-y', '-ar', '16000', outputFile]);
    const copyFile = spawn('cp', [tempFile, outputFile]);

    ffmpeg.childProcess.stdout.on('data', (data) => {
        console.log(`stdout: ${data}`);
    });

    ffmpeg.childProcess.stderr.on('data', (data) => {
        console.log(`stderr: ${data}`);
    });

    try {
        await ffmpeg;
        await copyFile;
        return outputFile;
    } catch (err) {
        console.log(err);
        return null;
    }
}

async function convertVideoToWavRPC(call, callback) {
    console.log(call.request);
    var outputFile;
    (async () => {
        outputFile = await convertVideoToWav(call.request.filePath);
        callback(null, { filePath: outputFile });
    })();
}

async function processVideo(pathToFile) {
    console.log("processVideo");
    var fileName = pathToFile.substring(pathToFile.lastIndexOf('/') + 1, pathToFile.lastIndexOf('.')) + '_processed.mp4';
    var outputFile = _dirname + "/" + fileName;
    var tempFile = "/tmp/" + fileName;
    const { spawn } = require('child-process-promise');
    const ffmpeg = spawn('ffmpeg', ['-nostdin', '-i', pathToFile, '-y', '-c:v', 'libx264' ,'-b:v',
    '500K', '-s', '768x432', '-movflags', 'faststart', '-ar', '48000', '-preset', 'medium', tempFile]);
    const copyFile = spawn('cp', [tempFile, outputFile]);

    ffmpeg.childProcess.stdout.on('data', (data) => {
        console.log(`stdout: ${data}`);
    });

    ffmpeg.childProcess.stderr.on('data', (data) => {
        console.log(`stderr: ${data}`);
    });

    try {
        await ffmpeg;
        await copyFile;
        return outputFile;
    } catch (err) {
        console.log(err);
        return null;
    }
}

async function processVideoRPC(call, callback) {
    console.log(call.request);
    var outputFile;
    (async () => {
        outputFile = await processVideo(call.request.filePath);
        callback(null, { filePath: outputFile });
    })();
}

module.exports = {
    asyncForEach: asyncForEach,
    convertMStoTime: convertMStoTime,
    stringToDate: stringToDate,
    runSynchronously: runSynchronously,
    getRandomString: getRandomString,
    convertVideoToWavRPC: convertVideoToWavRPC,
    processVideoRPC: processVideoRPC
}