using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskEngine.Tasks
{
    class WakeDownloaderTask : RabbitMQTask<String>
    {
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = "WakeDownloader";
        }
        public WakeDownloaderTask(RabbitMQ rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask)
        {
            Init(rabbitMQ);
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
        }
        protected async override Task OnConsume(string obj)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var period = DateTime.Now.AddMonths(-12);
                var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(p));
            }
        }
    }
}
