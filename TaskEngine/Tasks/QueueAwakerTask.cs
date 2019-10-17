using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskEngine.Tasks
{
    class QueueAwakerTask : RabbitMQTask<JObject>, IJob
    {
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private readonly DownloadMediaTask _downloadMediaTask;
        private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly TranscriptionTask _transcriptionTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly ProcessVideoTask _processVideoTask;

        public QueueAwakerTask() { }

        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.QueueAwaker, "_1");
        }
        public QueueAwakerTask(RabbitMQConnection rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask,
            DownloadMediaTask downloadMediaTask, ConvertVideoToWavTask convertVideoToWavTask, 
            TranscriptionTask transcriptionTask, ProcessVideoTask processVideoTask,
            GenerateVTTFileTask generateVTTFileTask)
        {
            Init(rabbitMQ);
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _downloadMediaTask = downloadMediaTask;
            _convertVideoToWavTask = convertVideoToWavTask;
            _transcriptionTask = transcriptionTask;
            _generateVTTFileTask = generateVTTFileTask;
            _processVideoTask = processVideoTask;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                Init((RabbitMQConnection)context.MergedJobDataMap["rabbitMQ"]);
                JObject msg = new JObject();
                msg.Add("Type", CommonUtils.TaskType.PeriodicCheck.ToString());
                Publish(msg);
            }
        }

        private async Task PendingJobs()
        {
            using (var context = CTDbContext.CreateDbContext())
            {

                // Medias for which no videos have downloaded
                context.Medias.Where(m => !m.Videos.Any()).ToList().ForEach(m => _downloadMediaTask.Publish(m));
                // Videos which haven't been converted to wav 
                context.Videos.Where(v => v.MediaId != null && v.AudioId == null).ToList().ForEach(v => _convertVideoToWavTask.Publish(v));
                // Videos which have failed in transcribing
                context.Videos.Where(v => v.TranscriptionStatus != "NoError" && v.MediaId != null && v.AudioId != null)
                    .ToList().ForEach(v => _transcriptionTask.Publish(v));
                // Completed Transcriptions which haven't generated vtt files
                context.Transcriptions.Where(t => t.Captions.Count > 0 && t.File == null).ToList().ForEach(t => _generateVTTFileTask.Publish(t));
            }            
        }

        private async Task DownloadAllPlaylists()
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var period = DateTime.Now.AddMonths(-12);
                var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(p));
            }
        }

        protected async override Task OnConsume(JObject jObject)
        {
            if (jObject["Type"].ToString() == CommonUtils.TaskType.PeriodicCheck.ToString())
            {
                await DownloadAllPlaylists();
                await PendingJobs();

            }
            else if(jObject["Type"].ToString() == CommonUtils.TaskType.DownloadAllPlaylists.ToString())
            {
                await DownloadAllPlaylists();
            }
            else if (jObject["Type"].ToString() == CommonUtils.TaskType.DownloadPlaylistInfo.ToString())
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    var playlistId = jObject["playlistId"].ToString();
                    var playlist = await _context.Playlists.FindAsync(playlistId);
                    _downloadPlaylistInfoTask.Publish(playlist);
                }
            }
            else if (jObject["Type"].ToString() == CommonUtils.TaskType.GenerateVTTFile.ToString())
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    var transcriptionId = jObject["TranscriptionId"].ToString();
                    var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                    _generateVTTFileTask.Publish(transcription);
                }
            }
        }
    }
}
