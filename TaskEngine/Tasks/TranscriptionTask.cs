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
        private GenerateVTTFileTask _generateVTTFileTask;
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(CommonUtils.TaskType.TranscribeMedia, "_1");
        }
        public TranscriptionTask(RabbitMQ rabbitMQ, RpcClient rpcClient, MSTranscriptionService msTranscriptionService, GenerateVTTFileTask generateVTTFileTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _msTranscriptionService = msTranscriptionService;
            _appSettings = Globals.appSettings;
            _generateVTTFileTask = generateVTTFileTask;
        }
        protected async override Task OnConsume(Video video)
        {
            var result = await _msTranscriptionService.RecognitionWithAudioStreamAsync(video.Audio.Path);
            List<Transcription> transcriptions = new List<Transcription>();
            foreach(var language in result.Item1)
            {
                if(language.Value.Count > 0)
                {
                    transcriptions.Add(new Transcription
                    {
                        Language = language.Key,
                        MediaId = video.MediaId,
                        Captions = language.Value
                    });
                }
            }
            using (var _context = CTDbContext.CreateDbContext())
            {                
                await _context.Transcriptions.AddRangeAsync(transcriptions);
                video.TranscriptionStatus = result.Item2;
                _context.Videos.Update(video);
                await _context.SaveChangesAsync();
            }
            transcriptions.ForEach(t => _generateVTTFileTask.Publish(t));
        }
    }
}
