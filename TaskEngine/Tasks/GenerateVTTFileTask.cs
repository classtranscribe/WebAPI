using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class GenerateVTTFileTask : RabbitMQTask<string>
    {
        public GenerateVTTFileTask(RabbitMQConnection rabbitMQ, ILogger<GenerateVTTFileTask> logger)
            : base(rabbitMQ, TaskType.GenerateVTTFile, logger)
        {

        }
        protected async override Task OnConsume(string transcriptionId, TaskParameters taskParameters)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                var captions = await (new CaptionQueries(_context)).GetCaptionsAsync(transcription.Id);
                var vttFile = Caption.GenerateWebVTTFile(captions, transcription.Language);
                var srtFile = Caption.GenerateSrtFile(captions);
                _context.Entry(transcription).State = EntityState.Modified;
                var vttfile = FileRecord.GetNewFileRecord(vttFile, ".vtt");
                var srtfile = FileRecord.GetNewFileRecord(srtFile, ".srt");
                await _context.FileRecords.AddAsync(srtfile);
                await _context.FileRecords.AddAsync(vttfile);
                transcription.File = vttfile;
                transcription.SrtFile = srtfile;
                await _context.SaveChangesAsync();
            }
        }
    }
}