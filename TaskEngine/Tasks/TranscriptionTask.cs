using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.MSTranscription;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task produces the transcriptions for a Video item.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class TranscriptionTask : RabbitMQTask<string>
    {
       
        private readonly MSTranscriptionService _msTranscriptionService;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly CaptionQueries _captionQueries;


        public TranscriptionTask(RabbitMQConnection rabbitMQ, MSTranscriptionService msTranscriptionService,
            GenerateVTTFileTask generateVTTFileTask, ILogger<TranscriptionTask> logger, CaptionQueries captionQueries)
            : base(rabbitMQ, TaskType.TranscribeVideo, logger)
        {
            _msTranscriptionService = msTranscriptionService;
            _generateVTTFileTask = generateVTTFileTask;
            _captionQueries = captionQueries;

        }
        private async void buildMockCaptions(string videoId)
        {
            GetLogger().LogInformation($"Building Mock Captions for video {videoId}");

            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.Include(v => v.Transcriptions).SingleAsync(v => v.Id == videoId);

                string[] languages = new string[] { Languages.ENGLISH_AMERICAN, Languages.SPANISH };
                foreach (var language in languages)
                {

                    var transcription = video.Transcriptions.SingleOrDefault(t => t.Language == language);
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
            registerTask(cleanup, videoId); // may throw AlreadyInProgress exception
            if (Globals.appSettings.MOCK_RECOGNITION == "MOCK")
            {
                buildMockCaptions(videoId);
            }


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
                string phraseHints = video.PhraseHints ?? "";
                
                GetLogger().LogInformation($"{videoId}:Using Phrase Hints length = {phraseHints.Length}");
                // GetKey can throw if the video.Id is currently being transcribed
                // However registerTask should have already detected that
                Key key = TaskEngineGlobals.KeyProvider.GetKey(video.Id);

                video.TranscribingAttempts += 10;
                await _context.SaveChangesAsync();

                try
                {
                    // create Dictionary and pass it to the recognition function
                    var captionsMap = new Dictionary<string, List<Caption>>();


                    // Set Source Language and Target (translation) Languages
                    var sourceLanguage = String.IsNullOrWhiteSpace(Globals.appSettings.SPEECH_RECOGNITION_DIALECT) ?
                        Languages.ENGLISH_AMERICAN : Globals.appSettings.SPEECH_RECOGNITION_DIALECT.Trim();

                    var translations = new List<string> { Languages.ENGLISH_AMERICAN, Languages.SIMPLIFIED_CHINESE, Languages.KOREAN, Languages.SPANISH, Languages.FRENCH };
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
                        var existing = await _captionQueries.GetCaptionsAsync(video.Id, language);
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


                    var result = await _msTranscriptionService.RecognitionWithVideoStreamAsync(videoId, video.Video1, key, captionsMap, sourceLanguage, phraseHints, startAfterMap);

                    TaskEngineGlobals.KeyProvider.ReleaseKey(key, video.Id);

                    foreach (var captionsInLanguage in result.Captions)
                    {
                        var theLanguage = captionsInLanguage.Key;
                        var theCaptions = captionsInLanguage.Value;

                        if (theCaptions.Any())
                        {
                            var t = _context.Transcriptions.SingleOrDefault(t => t.VideoId == video.Id && t.Language == theLanguage);
                            GetLogger().LogInformation($"Find Existing Transcriptions null={t == null}");
                            // Did we get the default or an existing Transcription entity?
                            if (t == null)
                            {
                                t = new Transcription()
                                {
                                    Captions = theCaptions,
                                    Language = theLanguage,
                                    VideoId = video.Id,
                                    Label = $"{theLanguage} (ClassTranscribe)",
                                    SourceInternalRef = "ClassTranscribe/Azure"
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


                    await _context.SaveChangesAsync();
                    // we now do the scene detection first because we want to complete the OCR and phrase list
                    //Not any more xxx_sceneDetectionTask.xxxPublish(video.Id);
                    video.Transcriptions.ForEach(t => _generateVTTFileTask.Publish(t.Id));
                     
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

