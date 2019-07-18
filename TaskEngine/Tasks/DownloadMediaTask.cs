using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class DownloadMediaTask : RabbitMQTask<Media>
    {
        private RpcClient _rpcClient;
        private ConvertVideoToWavTask _convertVideoToWavTask;

        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.DownloadMedia, "_1");
        }
        public DownloadMediaTask(RabbitMQ rabbitMQ, RpcClient rpcClient, ConvertVideoToWavTask convertVideoToWavTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _convertVideoToWavTask = convertVideoToWavTask;
        }

        protected override async Task OnConsume(Media media)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                Console.WriteLine("Consuming" + media);
                Video video = new Video();
                switch (media.SourceType)
                {
                    case SourceType.Echo360: video = await DownloadEchoVideo(media); break;
                    case SourceType.Youtube: video = await DownloadYoutubeVideo(media); break;
                    case SourceType.Local: video = await DownloadLocalPlaylist(media); break;
                }
                await _context.Videos.AddAsync(video);
                await _context.SaveChangesAsync();
                Console.WriteLine("Downloaded:" + video);
                _convertVideoToWavTask.Publish(video);
            }                
        }

        public async Task<Video> DownloadEchoVideo(Media media)
        {
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString(),
                AdditionalInfo = media.JsonMetadata["download_header"].ToString()
            });

            var mediaResponse2 = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["altVideoUrl"].ToString(),
                AdditionalInfo = media.JsonMetadata["download_header"].ToString()
            });

            Video video = new Video
            {
                Video1 = new FileRecord(mediaResponse.FilePath),
                Video2 = new FileRecord(mediaResponse2.FilePath),
                MediaId = media.Id
            };
            return video;
        }

        public async Task<Video> DownloadYoutubeVideo(Media media)
        {
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });

            Video video = new Video
            {
                Video1 = new FileRecord(mediaResponse.FilePath),
                MediaId = media.Id
            };
            return video;
        }

        public async Task<Video> DownloadLocalPlaylist(Media media)
        {
            throw new NotImplementedException();
        }
    }
}
