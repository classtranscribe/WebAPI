using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json.Linq;


using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;

using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task produces the transcriptions for a Video item.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class LocalTranscriptionTask : RabbitMQTask<string>
    {

        private readonly CaptionQueries _captionQueries;
        private readonly RpcClient _rpcClient;


        public LocalTranscriptionTask(RabbitMQConnection rabbitMQ, 
            RpcClient rpcClient,
            // GenerateVTTFileTask generateVTTFileTask, 
            ILogger<LocalTranscriptionTask> logger, CaptionQueries captionQueries)
            : base(rabbitMQ, TaskType.TranscribeVideo, logger)
        {
            _rpcClient = rpcClient;
            _captionQueries = captionQueries;
        }

         protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId); // may throw AlreadyInProgress exception
           
            const string SOURCEINTERNALREF= "ClassTranscribe/Local"; // Do not change me; this is a key inside the database
            // to indicate the source of the captions was this code
                        

            using (var _context = CTDbContext.CreateDbContext())
            {

                // TODO: taskParameters.Force should wipe all captions and reset the Transcription Status

                Video video = await _context.Videos.Include(v => v.Video1).Where(v => v.Id == videoId).FirstAsync();
                // ! Note the 'Include' ; we don't build the whole tree of related Entities

                if (video.TranscriptionStatus == Video.TranscriptionStatusMessages.NOERROR)
                {
                    GetLogger().LogInformation($"{videoId}:Skipping Transcribing of- already complete");
                    return;
                }
                var medias = await  _context.Medias.Include(m=>m.Playlist).Where(m=>m.VideoId == videoId && m.Playlist != null).ToListAsync();
                if(medias.Count == 0) {
                    GetLogger().LogInformation($"{videoId}:Skipping Transcribing - no media / playlist cares about this video");
                    return;
                }
               
                GetLogger().LogInformation($"{videoId}: Has new Phrase Hints: {video.HasPhraseHints()}");

                string phraseHints = "";
                if (video.HasPhraseHints()) {
                    var data = await _context.TextData.FindAsync(video.PhraseHintsDataId);
                    phraseHints = data.Text;
                } else
                { // deprecated
                    phraseHints = video.PhraseHints ?? "";
                }
                
                GetLogger().LogInformation($"{videoId}:Using Phrase Hints length = {phraseHints.Length}");
                // GetKey can throw if the video.Id is currently being transcribed
                // However registerTask should have already detected that
                var key = TaskEngineGlobals.KeyProvider.GetKey(video.Id);

                video.TranscribingAttempts += 10;
                await _context.SaveChangesAsync();
                GetLogger().LogInformation($"{videoId}: Updated TranscribingAttempts = {video.TranscribingAttempts}");
                try
                {

                    GetLogger().LogInformation($"{videoId}: Calling RecognitionWithVideoStreamAsync");
                    
                    var request = new CTGrpc.TranscriptionRequest
                    {
                        LogId = videoId,
                        FilePath = video.Video1.VMPath,
                        Model = "en",
                        Language = "en"
                        // PhraseHints = phraseHints,
                        // CourseHints = "",
                        // OutputLanguages = "en"
                    };
                    var jsonString = "";
                    try {
                        jsonString = (await _rpcClient.PythonServerClient.TranscribeAudioRPCAsync(request)).Json;
                     }
                    catch (RpcException e)
                    {
                        if (e.Status.StatusCode == StatusCode.InvalidArgument)
                        {
                            GetLogger().LogError($"TranscribeAudioRPCAsync=({videoId}):{e.Message}");
                        }
                        return;
                    } finally {
                        GetLogger().LogInformation($"{videoId} Transcribe - rpc complete");
                        TaskEngineGlobals.KeyProvider.ReleaseKey(key, video.Id);
                    }
                    
                    JObject jObject = JObject.Parse(jsonString);
                    // JArray jArray = JArray.Parse(jsonString);
                    var theLanguage = jObject["result"]["language"].ToString(Newtonsoft.Json.Formatting.None);
                    var theCaptionsAsJson = jObject["transcription"];

                    var theCaptions = new List<Caption>();
                    int cueCount = 0; 
                        
                    foreach (var jsonCue in theCaptionsAsJson) { 
                        var caption = new Caption() {
                            Index  = cueCount ++,
                            Begin = TimeSpan.Parse(jsonCue["timestamps"]["from"].ToString(Newtonsoft.Json.Formatting.None).Replace(",",".")),
                            End = TimeSpan.Parse(jsonCue["timestamps"]["to"].ToString(Newtonsoft.Json.Formatting.None).Replace(",",".")) ,
                            Text = jsonCue["text"] .ToString(Newtonsoft.Json.Formatting.None).Trim()
                        };

                        theCaptions.Add(caption);
                    }
                    if (theCaptions.Count > 0)
                    {
                        GetLogger().LogInformation($"{videoId}: Created {theCaptions.Count} captions objects"); 

                        var t = _context.Transcriptions.SingleOrDefault(t => t.VideoId == video.Id && t.SourceInternalRef == SOURCEINTERNALREF && t.Language == theLanguage && t.TranscriptionType == TranscriptionType.Caption);
                        GetLogger().LogInformation($"Find Existing Transcriptions null={t == null}");
                        // Did we get the default or an existing Transcription entity?
                        if (t == null)
                        {
                            t = new Transcription()
                            {
                                TranscriptionType = TranscriptionType.Caption,
                                Captions = theCaptions,
                                Language = theLanguage,
                                VideoId = video.Id,
                                Label = $"{theLanguage} (ClassTranscribe)",
                                SourceInternalRef = SOURCEINTERNALREF, // 
                                SourceLabel = "ClassTranscribe (Local" + (phraseHints.Length>0 ?" with phrase hints)" : ")")
                                // Todo store the entire Whisper result here
                            };
                            _context.Add(t);
                        }
                        else
                        {
                            t.Captions.AddRange(theCaptions);
                        }
                    }
                    

                    video.TranscriptionStatus = "NoError";
                    // video.JsonMetadata["LastSuccessfulTime"] = result.LastSuccessTime.ToString();

                    GetLogger().LogInformation($"{videoId}: Saving captions"); 
                    await _context.SaveChangesAsync();                     
                }
                catch (Exception ex)
                {
                    GetLogger().LogError(ex, $"{videoId}: Transcription Exception:${ex.StackTrace}");
                    video.TranscribingAttempts += 1000;
                    await _context.SaveChangesAsync();
                    throw;
                }

            }
        }

    }
}