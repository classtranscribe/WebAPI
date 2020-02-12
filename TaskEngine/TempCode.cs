using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskEngine.Tasks;

namespace TaskEngine
{
    class TempCode
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext context;
        private readonly CreateBoxTokenTask _createBoxTokenTask;
        private readonly UpdateBoxTokenTask _updateBoxTokenTask;
        private readonly EPubGeneratorTask _ePubGeneratorTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly TranscriptionTask _transcriptionTask;
        private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly DownloadMediaTask _downloadMediaTask;
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;

        public TempCode(CTDbContext c, CreateBoxTokenTask createBoxTokenTask, UpdateBoxTokenTask updateBoxTokenTask,
            EPubGeneratorTask ePubGeneratorTask, ProcessVideoTask processVideoTask, GenerateVTTFileTask generateVTTFileTask,
            TranscriptionTask transcriptionTask, ConvertVideoToWavTask convertVideoToWavTask, DownloadMediaTask downloadMediaTask,
            DownloadPlaylistInfoTask downloadPlaylistInfoTask)
        {
            context = c;
            _createBoxTokenTask = createBoxTokenTask;
            _updateBoxTokenTask = updateBoxTokenTask;
            _ePubGeneratorTask = ePubGeneratorTask;
            _processVideoTask = processVideoTask;
            _generateVTTFileTask = generateVTTFileTask;
            _transcriptionTask = transcriptionTask;
            _convertVideoToWavTask = convertVideoToWavTask;
            _downloadMediaTask = downloadMediaTask;
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
        }

        public void CleanUpInvalidVideos()
        {
            var incompleteVideos = context.Videos.Select(v => new
            {
                Length = File.Exists(v.Video1.Path) ? new System.IO.FileInfo(v.Video1.Path).Length : -1,
                Path = v.Video1.Path,
                Video = v
            }).ToList().Where(v => v.Length < 1000).OrderBy(v => v.Length).Select(v => new { v.Video, v.Length }).ToList();

            foreach (var item in incompleteVideos)
            {
                if (File.Exists(item.Video.Video1.Path))
                {
                    File.Delete(item.Video.Video1.Path);
                }
                context.FileRecords.Remove(item.Video.Video1);
                if (item.Video.Video2 != null)
                {
                    if (File.Exists(item.Video.Video2.Path))
                    {
                        File.Delete(item.Video.Video2.Path);
                    }
                    context.FileRecords.Remove(item.Video.Video2);
                }
                if (item.Video.Audio != null)
                {
                    if (File.Exists(item.Video.Audio.Path))
                    {
                        File.Delete(item.Video.Audio.Path);
                    }
                    context.FileRecords.Remove(item.Video.Audio);
                }
                context.Videos.Remove(item.Video);
            }
            context.SaveChanges();
        }

        public void UpdateMediaNames()
        {
            List<Media> medias;

            medias = context.Medias.ToList();
            foreach (Media media in medias)
            {
                media.Name = DownloadPlaylistInfoTask.GetMediaName(media);
                Console.WriteLine(media.Name);
            }
            context.SaveChanges();
        }
    }
}
