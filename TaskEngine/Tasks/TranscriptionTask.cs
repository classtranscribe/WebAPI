using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using TaskEngine.MSTranscription;

namespace TaskEngine.Tasks
{
    class TranscriptionTask : RabbitMQTask<Video>
    {
        private RpcClient _rpcClient;
        private MSTranscriptionService _msTranscriptionService;
        private AppSettings _appSettings;
        private void Init(RabbitMQ rabbitMQ, CTDbContext context)
        {
            _rabbitMQ = rabbitMQ;
            _context = context;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.TranscribeMedia, "_1");
        }
        public TranscriptionTask(RabbitMQ rabbitMQ, CTDbContext context, RpcClient rpcClient, MSTranscriptionService msTranscriptionService, IOptions<AppSettings> appSettings)
        {
            Init(rabbitMQ, context);
            _rpcClient = rpcClient;
            _msTranscriptionService = msTranscriptionService;
            _appSettings = appSettings.Value;
        }
        protected async override Task OnConsume(Video video)
        {
            var audioFilePath = video.AudioPath.Substring(video.AudioPath.IndexOf("Data/") + 5);
            string path = Path.Combine(_appSettings.DATA_DIRECTORY, audioFilePath);
            Transcription t = new Transcription
            {
                Path = await _msTranscriptionService.RecognitionWithAudioStreamAsync(path),
                MediaId = video.MediaId
            };
            await _context.Transcriptions.AddAsync(t);
            await _context.SaveChangesAsync();
        }
    }
}
