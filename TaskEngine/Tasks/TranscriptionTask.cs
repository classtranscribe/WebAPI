using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System.Collections.Generic;
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
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.TranscribeMedia, "_1");
        }
        public TranscriptionTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, MSTranscriptionService msTranscriptionService, GenerateVTTFileTask generateVTTFileTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _msTranscriptionService = msTranscriptionService;
            _appSettings = Globals.appSettings;
            _generateVTTFileTask = generateVTTFileTask;
        }
        protected async override Task OnConsume(Video video)
        {
            var result = await _msTranscriptionService.RecognitionWithAudioStreamAsync(video);
            List<Transcription> transcriptions = new List<Transcription>();
            foreach(var language in result.Item1)
            {
                if(language.Value.Count > 0)
                {
                    transcriptions.Add(new Transcription
                    {
                        Language = language.Key,
                        VideoId = video.Id,
                        Captions = language.Value
                    });
                }
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                var latestVideo = await _context.Videos.FindAsync(video.Id);
                if (latestVideo.TranscriptionStatus != "NoError")
                {
                    await _context.Transcriptions.AddRangeAsync(transcriptions);
                    latestVideo.TranscriptionStatus = result.Item2;
                    latestVideo.TranscribingAttempts += 1;
                    await _context.SaveChangesAsync();
                    transcriptions.ForEach(t => _generateVTTFileTask.Publish(t));
                }
            }            
        }
    }
}
