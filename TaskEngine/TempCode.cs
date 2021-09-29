using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using TaskEngine.Tasks;

namespace TaskEngine
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class TempCode
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext context;
        private readonly CreateBoxTokenTask _createBoxTokenTask;
        private readonly UpdateBoxTokenTask _updateBoxTokenTask;
        private readonly SceneDetectionTask _sceneDetectionTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly TranscriptionTask _transcriptionTask;
        private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly DownloadMediaTask _downloadMediaTask;
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private readonly RpcClient _rpcClient;
        private readonly QueueAwakerTask _queueAwakerTask;
        private readonly CleanUpElasticIndexTask _cleanUpElasticIndexTask;

        public TempCode(CTDbContext c, CreateBoxTokenTask createBoxTokenTask, UpdateBoxTokenTask updateBoxTokenTask,
            SceneDetectionTask ePubGeneratorTask, ProcessVideoTask processVideoTask, GenerateVTTFileTask generateVTTFileTask,
            TranscriptionTask transcriptionTask, ConvertVideoToWavTask convertVideoToWavTask, DownloadMediaTask downloadMediaTask,
            DownloadPlaylistInfoTask downloadPlaylistInfoTask, QueueAwakerTask queueAwakerTask,
            CleanUpElasticIndexTask cleanUpElasticIndexTask, RpcClient rpcClient)
        {
            context = c;
            _createBoxTokenTask = createBoxTokenTask;
            _updateBoxTokenTask = updateBoxTokenTask;
            _sceneDetectionTask = ePubGeneratorTask;
            _processVideoTask = processVideoTask;
            _generateVTTFileTask = generateVTTFileTask;
            _transcriptionTask = transcriptionTask;
            _convertVideoToWavTask = convertVideoToWavTask;
            _downloadMediaTask = downloadMediaTask;
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _queueAwakerTask = queueAwakerTask;
            _rpcClient = rpcClient;
            _cleanUpElasticIndexTask = cleanUpElasticIndexTask;
        }

        public void CleanUpInvalidVideos()
        {
            var incompleteVideos = context.Videos.Select(v => new
            {
                Length = File.Exists(v.Video1.Path) ? new System.IO.FileInfo(v.Video1.Path).Length : -1,
                v.Video1.Path,
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

        public void Temp()
        {
            TempAsync().GetAwaiter().GetResult();
        }

        private async Task TempAsync()
        {
            // A dummy awaited function call.
            await Task.Delay(0);
            // Add any temporary code
            _cleanUpElasticIndexTask.Publish("");
        }

        private async Task TestYoutubeChannelDownload()
        {
            Playlist p = new Playlist
            {
                JsonMetadata = new JObject
                {
                    {"isChannel","1" }
                },
                PlaylistIdentifier = "UCi8e0iOVk1fEOogdfu4YgfA" // change me - too many videos
            };
            context.Playlists.Add(p);
            await context.SaveChangesAsync();
            //p.Id = "72336862-244c-4904-a753-2f620ae99633";
            _downloadPlaylistInfoTask.Publish(p.Id);

        }
    }
}
