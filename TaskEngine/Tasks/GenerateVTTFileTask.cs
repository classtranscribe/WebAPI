using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
