using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Options;
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
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.TranscribeMedia, "_1");
        }
        public TranscriptionTask(RabbitMQ rabbitMQ, RpcClient rpcClient, MSTranscriptionService msTranscriptionService)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _msTranscriptionService = msTranscriptionService;
            _appSettings = CTDbContext.appSettings;
        }
        protected async override Task OnConsume(Video video)
        {
            Transcription t = new Transcription
            {
                File = new FileRecord(await _msTranscriptionService.RecognitionWithAudioStreamAsync(video.Audio.Path)),
                MediaId = video.MediaId
            };
            using (var _context = CTDbContext.CreateDbContext())
            {                
                await _context.Transcriptions.AddAsync(t);
                await _context.SaveChangesAsync();
            }
        }
    }
}
