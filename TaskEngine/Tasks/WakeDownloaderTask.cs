using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskEngine.Tasks
{
    class WakeDownloaderTask : RabbitMQTask<JObject>
    {
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = "WakeDownloader";
        }
        public WakeDownloaderTask(RabbitMQ rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask, GenerateVTTFileTask generateVTTFileTask)
        {
            Init(rabbitMQ);
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _generateVTTFileTask = generateVTTFileTask;
        }
        protected async override Task OnConsume(JObject jObject)
        {
            if(jObject["Type"].ToString() == CommonUtils.TaskType.DownloadAllPlaylists.ToString())
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    var period = DateTime.Now.AddMonths(-12);
                    var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                    playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(p));
                }
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
