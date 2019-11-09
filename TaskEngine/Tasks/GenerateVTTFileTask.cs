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
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.GenerateVTTFile, "_1");
        }

        public GenerateVTTFileTask(RabbitMQConnection rabbitMQ)
        {
            Init(rabbitMQ);
        }
        protected async override Task OnConsume(Transcription t)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var transcription = await _context.Transcriptions.FindAsync(t.Id);
                var captions = await _context.Captions.Where(c => c.TranscriptionId == transcription.Id)
                .GroupBy(c => c.Index).Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .OrderBy(c => c.Index).ToListAsync();
                var audioPath = transcription.Video.Audio.Path;
                var vttFile = Caption.GenerateWebVTTFile(captions, audioPath, transcription.Language);
                var srtFile = Caption.GenerateSrtFile(captions, audioPath, transcription.Language);
                _context.Entry(transcription).State = EntityState.Modified;
                transcription.File = new FileRecord(vttFile);
                transcription.SrtFile = new FileRecord(srtFile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
