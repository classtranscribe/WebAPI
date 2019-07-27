using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            _appSettings = Globals.appSettings;
        }
        protected async override Task OnConsume(Video video)
        {
            var result = await _msTranscriptionService.RecognitionWithAudioStreamAsync(video.Audio.Path);
            List<Transcription> transcriptions = new List<Transcription>();
            foreach(var language in result.Item2)
            {
                var captions = result.Item1[language.Key].Select(s => s.ToCaption()).ToList();
                int i = 1;
                captions.ForEach(c => c.Index = i++);
                transcriptions.Add(new Transcription
                {
                    File = new FileRecord(language.Value),
                    Language = language.Key,
                    MediaId = video.MediaId,
                    Captions = captions
                });
            }
            using (var _context = CTDbContext.CreateDbContext())
            {                
                await _context.Transcriptions.AddRangeAsync(transcriptions);
                await _context.SaveChangesAsync();
            }
        }
    }
}
