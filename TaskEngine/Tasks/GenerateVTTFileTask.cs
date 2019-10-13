using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class GenerateVTTFileTask : RabbitMQTask<Transcription>
    {
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(CommonUtils.TaskType.GenerateVTTFile, "_1");
        }

        public GenerateVTTFileTask(RabbitMQ rabbitMQ)
        {
            Init(rabbitMQ);
        }
        protected async override Task OnConsume(Transcription transcription)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var captions = await _context.Captions.Where(c => c.TranscriptionId == transcription.Id)
                .GroupBy(c => c.Index).Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .OrderBy(c => c.Index).ToListAsync();
                var audioPath = await _context.Medias.Where(m => m.Id == transcription.MediaId).Select(m => m.Videos.First().Audio.Path).FirstAsync();
                var vttFile = Caption.GenerateWebVTTFile(captions, audioPath, transcription.Language);
                var srtFile = Caption.GenerateSrtFile(captions, audioPath, transcription.Language);
                _context.Entry(transcription).State = EntityState.Modified;
                transcription.File = new FileRecord(vttFile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
