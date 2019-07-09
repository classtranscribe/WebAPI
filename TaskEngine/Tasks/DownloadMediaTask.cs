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

        private void Init(RabbitMQ rabbitMQ, CTDbContext context)
        {
            _rabbitMQ = rabbitMQ;
            _context = context;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.DownloadMedia, "_1");
        }
        public DownloadMediaTask(RabbitMQ rabbitMQ, CTDbContext context, RpcClient rpcClient, ConvertVideoToWavTask convertVideoToWavTask)
        {
            Init(rabbitMQ, context);
            _rpcClient = rpcClient;
            _convertVideoToWavTask = convertVideoToWavTask;
        }

        protected override async Task OnConsume(Media media)
        {
            Console.WriteLine("Consuming" + media);
            Video video = new Video();
            switch (media.SourceType)
            {
                case SourceType.Echo360: video = await DownloadEchoVideo(media); break;
                case SourceType.Youtube: video = await DownloadYoutubeVideo(media); break;
                case SourceType.Local: video = await DownloadLocalPlaylist(media); break;
            }
            Console.WriteLine("Downloaded:" + video);
            _convertVideoToWavTask.Publish(video);
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
                Video1Path = mediaResponse.FilePath,
                Video2Path = mediaResponse2.FilePath,
                MediaId = media.Id
            };
            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
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
                Video1Path = mediaResponse.FilePath,
                MediaId = media.Id
            };
            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
            return video;
        }

        public async Task<Video> DownloadLocalPlaylist(Media media)
        {
            throw new NotImplementedException();
        }
    }
}
