using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread



namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class GenerateVTTFileTask : RabbitMQTask<string>
    {
        public GenerateVTTFileTask(RabbitMQConnection rabbitMQ,
            ILogger<GenerateVTTFileTask> logger)
            : base(rabbitMQ, TaskType.GenerateVTTFile, logger)
        {
        }
        protected async override Task OnConsume(string transcriptionId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, transcriptionId); // may throw AlreadyInProgress exception
            
            GetLogger().LogInformation($"Creating VTT & SRT files for ({transcriptionId}) - nope");
            return;


            using (var _context = CTDbContext.CreateDbContext())
            {
                var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                string subdir = ToCourseOfferingSubDirectory(_context, transcription);
                CaptionQueries captionQueries = new CaptionQueries(_context);
                var captions = await captionQueries.GetCaptionsAsync(transcription.Id);

                var vttfile = await FileRecord.GetNewFileRecordAsync(Caption.GenerateWebVTTFile(captions, transcription.Language), ".vtt", subdir);

#nullable enable
                FileRecord? existingVtt = await _context.FileRecords.FindAsync(transcription.FileId);
#nullable disable

                if (existingVtt is null)
                {
                    GetLogger().LogInformation($"{transcriptionId}: Creating new vtt file {vttfile.FileName}"); 
                    await _context.FileRecords.AddAsync(vttfile);
                    transcription.File = vttfile;
                    _context.Entry(transcription).State = EntityState.Modified;
                }
                else
                {   
                    GetLogger().LogInformation($"{transcriptionId}: replacing existing vtt file contents {existingVtt.FileName}");              
                    existingVtt.ReplaceWith(vttfile);
                    _context.Entry(existingVtt).State = EntityState.Modified;
                }

                var srtfile = await FileRecord.GetNewFileRecordAsync(Caption.GenerateSrtFile(captions), ".srt", subdir);

#nullable enable
                FileRecord? existingSrt = await _context.FileRecords.FindAsync(transcription.SrtFileId);
#nullable disable

                if (existingSrt is null)
                {
                    GetLogger().LogInformation($"{transcriptionId}: Creating new srt file {srtfile.FileName}"); 

                    await _context.FileRecords.AddAsync(srtfile);
                    transcription.SrtFile = srtfile;
                    _context.Entry(transcription).State = EntityState.Modified;
                }
                else
                {
                    GetLogger().LogInformation($"{transcriptionId}: replacing existing srt file contents {existingSrt.FileName}");              
                    existingSrt.ReplaceWith(srtfile);
                    _context.Entry(existingSrt).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                GetLogger().LogInformation($"{transcriptionId}: Database updated");   
            }
        }
    }
}