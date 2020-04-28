using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using CTCommons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class GenerateVTTFileTask : RabbitMQTask<string>
    {
        public GenerateVTTFileTask(RabbitMQConnection rabbitMQ, 
            ILogger<GenerateVTTFileTask> logger)
            : base(rabbitMQ, TaskType.GenerateVTTFile, logger)
        {
        }
        protected async override Task OnConsume(string transcriptionId, TaskParameters taskParameters)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                CaptionQueries captionQueries = new CaptionQueries(_context);
                var captions = await captionQueries.GetCaptionsAsync(transcription.Id);

                var vttfile = FileRecord.GetNewFileRecord(Caption.GenerateWebVTTFile(captions, transcription.Language), ".vtt");
                await _context.FileRecords.AddAsync(vttfile);
                transcription.File = vttfile;

                var srtfile = FileRecord.GetNewFileRecord(Caption.GenerateSrtFile(captions), ".srt");
                await _context.FileRecords.AddAsync(srtfile);
                transcription.SrtFile = srtfile;

                _context.Entry(transcription).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
    }
}