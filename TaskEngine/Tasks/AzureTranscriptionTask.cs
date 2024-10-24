﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using ClassTranscribeDatabase.Services.MSTranscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
    class AzureTranscriptionTask : RabbitMQTask<string>
    {
       
        private readonly MSTranscriptionService _msTranscriptionService;
        // nope private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly CaptionQueries _captionQueries;


        public AzureTranscriptionTask(RabbitMQConnection rabbitMQ, MSTranscriptionService msTranscriptionService,
            // GenerateVTTFileTask generateVTTFileTask, 
            ILogger<AzureTranscriptionTask> logger, CaptionQueries captionQueries)
            : base(rabbitMQ, TaskType.AzureTranscribeVideo, logger)
        {
            _msTranscriptionService = msTranscriptionService;
            // nope _generateVTTFileTask = generateVTTFileTask;
            _captionQueries = captionQueries;

        }
        private async Task buildMockCaptions(string videoId)
        {
            GetLogger().LogInformation($"Building Mock Captions for video {videoId}");

            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.Include(v => v.Transcriptions).SingleAsync(v => v.Id == videoId);

                string[] languages = new string[] { Languages.ENGLISH_AMERICAN, Languages.SPANISH };
                foreach (var language in languages)
                {

                    var transcription = video.Transcriptions.SingleOrDefault(t => t.Language == language && t.TranscriptionType == TranscriptionType.Caption);
                    // Did we get the default or an existing Transcription entity?
                    if (transcription == null)
                    {
                        transcription = new Transcription() { Language = language, VideoId = video.Id };
                        _context.Add(transcription);
                    };

                    TimeSpan time = new TimeSpan();

                    TimeSpan duration = new TimeSpan(0, 0, 3); // seconds

                    for (int index = 1; index <= 3; index++)
                    {
                        TimeSpan end = time.Add(duration);

                        Caption c = new Caption
                        {
                            Index = index,
                            Text = $"The Caption in {language} is {index + 100} on {DateTime.Now}",
                            Begin = time,
                            End = end,
                            TranscriptionId = transcription.Id

                        };
                        _context.Add(c);
                        time = end;
                    } // for


                } // for language
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// [2020.7.7] For the purpose of resuming failed transcriptions, all the captions of the failed transcription would now be stored in the database.
        /// Before resuming, all the old captions would be queried out from the database and stored in a dictionary, which will be passed to the
        /// transcription function along with the resume time point.
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="taskParameters"></param>
        /// <returns></returns>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId); // may throw AlreadyInProgress exception
            if (Globals.appSettings.MOCK_RECOGNITION == "MOCK")
            {
                await buildMockCaptions(videoId);
            }
            const string SOURCEINTERNALREF= "ClassTranscribe/Azure"; // Do not change me; this is a key inside the database
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
                
                string doAzure = "";

                foreach(var media in medias) {
                    doAzure += media.GetOptionsAsJson().GetValue("doAzureCaptions")?.ToString() ?? "1";
                }
                if(! doAzure.Contains("1")) {
                    GetLogger().LogInformation($"{videoId}:Skipping Transcribing - no one requested Azure transcription");
                    return;
                }
           
                // video.PhraseHint is deprecated
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
                Key key = TaskEngineGlobals.KeyProvider.GetKey(video.Id);

                video.TranscribingAttempts += 10;
                await _context.SaveChangesAsync();
                GetLogger().LogInformation($"{videoId}: Updated TranscribingAttempts = {video.TranscribingAttempts}");
                try
                {
                    // create Dictionary and pass it to the recognition function
                    var captionsMap = new Dictionary<string, List<Caption>>();


                    // Set Source Language and Target (translation) Languages
                    var sourceLanguage = String.IsNullOrWhiteSpace(Globals.appSettings.SPEECH_RECOGNITION_DIALECT) ?
                        Languages.ENGLISH_AMERICAN : Globals.appSettings.SPEECH_RECOGNITION_DIALECT.Trim();

                    var translations = new List<string> { Languages.ENGLISH_AMERICAN };//, Languages.SIMPLIFIED_CHINESE, Languages.KOREAN, Languages.SPANISH, Languages.FRENCH };
                    if (!String.IsNullOrWhiteSpace(Globals.appSettings.LANGUAGE_TRANSLATIONS))
                    {
                        translations = Globals.appSettings.LANGUAGE_TRANSLATIONS.Split(',').ToList();
                    }
                    GetLogger().LogInformation($"{videoId}: ({sourceLanguage}). Translation(s) = ({String.Join(',', translations)})");


                    // Different languages may not be as complete
                    // So find the minimum timespan of the max observed ending for each language
                    TimeSpan shortestTime = TimeSpan.MaxValue; // Cant use TimeSpan.Zero to mean unset
                    var startAfterMap = new Dictionary<string, TimeSpan>();

                    var allLanguages = new List<string>(translations);
                    allLanguages.Add(sourceLanguage);

                    foreach (string language in allLanguages)
                    {
                        var existing = await _captionQueries.GetCaptionsAsync(video.Id, SOURCEINTERNALREF, language);
                        captionsMap[language] = existing;

                        startAfterMap[language] = TimeSpan.Zero; 
                        if (existing.Any())
                        {
                            TimeSpan lastCaptionTime = existing.Select(c => c.End).Max();
                            startAfterMap[language] = lastCaptionTime;

                            GetLogger().LogInformation($"{ videoId}:{language}. Last Caption at {lastCaptionTime}");
                        }
                    }


                    //var lastSuccessTime = shortestTime < TimeSpan.MaxValue ? shortestTime : TimeSpan.Zero;
                    //if (video.JsonMetadata != null && video.JsonMetadata["LastSuccessfulTime"] != null)
                    //{
                    //    lastSuccessTime = TimeSpan.Parse(video.JsonMetadata["LastSuccessfulTime"].ToString());
                    //}

                    GetLogger().LogInformation($"{videoId}: Calling RecognitionWithVideoStreamAsync");
                    var result = await _msTranscriptionService.RecognitionWithVideoStreamAsync(videoId, video.Video1, key, captionsMap, sourceLanguage, phraseHints, startAfterMap);

                    GetLogger().LogInformation($"{videoId}: Finished RecognitionWithVideoStreamAsync - Releasing Key");

                    TaskEngineGlobals.KeyProvider.ReleaseKey(key, video.Id);

                    foreach (var captionsInLanguage in result.Captions)
                    {
                        var theLanguage = captionsInLanguage.Key;
                        var theCaptions = captionsInLanguage.Value;
                        if (theCaptions.Count>0)
                        {
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
                                    SourceLabel = "ClassTranscribe (Azure" + (phraseHints.Length>0 ?" with phrase hints)" : ")")
                                };
                                _context.Add(t);
                            }
                            else
                            {
                                // Transcriptions already existed, we are just completing them, so add only the new ones
                                // TODO/TOREVIEW: Does this filter actually help, if existing caption entries were edited by hand?
                                var newCaptions = theCaptions.Where(c => c.Id == null);

                                t.Captions.AddRange(newCaptions);
                            }
                        }
                    }


                    video.TranscriptionStatus = result.ErrorCode;
                    video.JsonMetadata["LastSuccessfulTime"] = result.LastSuccessTime.ToString();

                    GetLogger().LogInformation($"{videoId}: Saving captions Code={result.ErrorCode}. LastSuccessTime={result.LastSuccessTime}"); 
                    await _context.SaveChangesAsync();
                   // nope, no vtt files  video.Transcriptions.ForEach(t => _generateVTTFileTask.Publish(t.Id));
                     
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

