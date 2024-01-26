using ClassTranscribeDatabase.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


//TODO: Cognitive services provides a list of languages that are available for detection and for translation
// Auto language detection is also possibe
// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-languages
// e.g. 
// https://api.cognitive.microsofttranslator.com/languages?api-version=3.0
// Speech recognition list is here - 
// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#speech-translation
namespace ClassTranscribeDatabase.Services.MSTranscription
{

    public class MSTranscriptionService
    {

        // Theses were generated manually on 2020/10/1 using curl and some regex replacements in vi
        private static readonly List<string> _supportedTranslations = new List<string>(
            "af,ar,as,bg,bn,bs,ca,cs,cy,da,de,el,en,es,et,fa,fi,fil,fj,fr,ga,gu,he,hi,hr,ht,hu,id,is,it,ja,kk,kmr,kn,ko,ku,lt,lv,mg,mi,ml,mr,ms,mt,mww,nb,nl,or,otq,pa,pl,prs,ps,pt,pt-pt,ro,ru,sk,sl,sm,sr-Cyrl,sr-Latn,sv,sw,ta,te,th,tlh-Latn,tlh-Piqd,to,tr,ty,uk,ur,vi,yua,yue,zh-Hans,zh-Hant".Split(','));

        private static readonly List<string> _supportedRecognition = new List<string>(
            "ar-AE,ar-BH,ar-EG,ar-IQ,ar-JO,ar-KW,ar-LB,ar-OM,ar-QA,ar-SA,ar-SY,bg-BG,ca-ES,cs-CZ,da-DK,de-DE,el-GR,en-AU,en-CA,en-GB,en-HK,en-IE,en-IN,en-NZ,en-PH,en-SG,en-US,en-ZA,es-AR,es-BO,es-CL,es-CO,es-CR,es-CU,es-DO,es-EC,es-ES,es-GT,es-HN,es-MX,es-NI,es-PA,es-PE,es-PR,es-PY,es-SV,es-US,es-UY,es-VE,et-EE,fi-FI,fr-CA,fr-FR,ga-IE,gu-IN,hi-IN,hr-HR,hu-HU,it-IT,ja-JP,ko-KR,lt-LT,lv-LV,mr-IN,mt-MT,nb-NO,nl-NL,pl-PL,pt-BR,pt-PT,ro-RO,ru-RU,sk-SK,sl-SI,sv-SE,ta-IN,te-IN,th-TH,tr-TR,zh-CN,zh-HK,zh-TW".Split(',')
        );
        private readonly ILogger _logger;
        private readonly SlackLogger _slackLogger;
        private readonly RpcClient _rpcClient;

        public class MSTResult
        {
            public Dictionary<string, List<Caption>> Captions { get; set; }
            public string ErrorCode { get; set; }
            public TimeSpan LastSuccessTime { get; set; }
        }

        public MSTranscriptionService(ILogger<MSTranscriptionService> logger, SlackLogger slackLogger, RpcClient rpcClient)
        {
            _logger = logger;
            _slackLogger = slackLogger;
            _rpcClient = rpcClient;
        }

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, FileRecord videoFile, Key key, Dictionary<string, List<Caption>> captions, string sourceLanguage, string phraseHints, Dictionary<string, TimeSpan> startAfterMap)
        {
            return await RecognitionWithVideoStreamAsync(logId, videoFile.VMPath, key, captions, sourceLanguage, phraseHints, startAfterMap);
        }
        /// <summary>
        /// Returns true if Cognitive Services supports this language code for recognition. Must match exactly in correct case e.g. en-US
        /// Not "en" or "en-us"
        /// </summary>
        /// <param name="dialect"></param>
        /// <returns>true if is this a valid language code for recognition</returns>
        private static bool IsSupportedRecognition(string dialect)
        {
            return _supportedRecognition.Contains(dialect);
        }
        private static bool IsSupportedTranslation(string language)
        {
            return _supportedTranslations.Contains(language);
        }

        //TODO/TOREVIEW: Refactor this method into setup/process/completion 
        // It is too long
        private void QuietDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deleting {1} failed", file);
                // do not rethrow
            }
        }
        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, string videoFilePath, Key key, Dictionary<string, List<Caption>> captions, string sourceLanguage, string phraseHints, Dictionary<string, TimeSpan> startAfterMap)
        {

            List<string> outputLanguages = startAfterMap.Keys.ToList<string>();
            TimeSpan restartOffset = startAfterMap.Any() ? startAfterMap.Values.Min() : TimeSpan.Zero;

            _logger.LogInformation($"{logId}:RecognitionWithVideoStreamAsync restartOffset=({restartOffset.TotalSeconds}) seconds");

            var trimmedAudioFile = await createTrimmedAudioFileAsync(videoFilePath, (float)restartOffset.TotalSeconds);

            try
            {

                _logger.LogInformation($"{logId}:createSpeechTranslationConfig");
                SpeechTranslationConfig speechConfig = createSpeechTranslationConfig(logId, key, sourceLanguage, outputLanguages);

                _logger.LogInformation($"{logId}:performRecognitionAsync ...");
                var result = await performRecognitionAsync(logId, trimmedAudioFile.FilePath, speechConfig, restartOffset,
                    sourceLanguage, captions, phraseHints, startAfterMap);

                return result;

            }
            finally
            {
                _logger.LogInformation($"{logId}:QuietDelete ({trimmedAudioFile.FilePath})");
                QuietDelete(trimmedAudioFile.FilePath);
            }

        }

        private async Task<MSTResult> performRecognitionAsync(string logId, string filePath, SpeechTranslationConfig speechConfig, TimeSpan restartOffset,
            string sourceLanguage, Dictionary<string, List<Caption>> captions, string phraseHints, Dictionary<string, TimeSpan> startAfterMap)
        {
            using (var audioInput = WavHelper.OpenWavFile(filePath))
            {
                var logOnce = new HashSet<string>();
                var stopRecognition = new TaskCompletionSource<int>();
                bool verboseLogging = true;
                TimeSpan lastSuccessfulTime = TimeSpan.Zero;
                string errorCode = "";
                var phrasesHintUsedCount = 0;
                using (var recognizer = new TranslationRecognizer(speechConfig, audioInput))
                {
                    //  PhraseList 
                    if( phraseHints.Length > 0) {
                        var grammar = PhraseListGrammar.FromRecognizer(recognizer);

                        var phrase_payload = 0;
                       
                        foreach (var phrase in phraseHints.Split('\n')){
                            // conservative estimate byte requirements usng UTF-16, plus 16 bytes for list item overhead
                            phrase_payload += 2 * phrase.Length + 16; 
                            if(phrase_payload >= 1<<16) {
                                _logger.LogInformation($"{logId}: phrase hints exceeded estimaged maximum phrase list byte limit; ignoring remainder");
                                break;
                            } 
                            if(phrase.Length > 0) {
                                grammar.AddPhrase(phrase);
                                phrasesHintUsedCount++;
                            }
                        }
                    }
                    _logger.LogInformation($"{logId}:PhraseHintsUsed:=({phrasesHintUsedCount})");

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            JObject jObject = JObject.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult));
                            var wordLevelCaptions = jObject["Words"]
                            .ToObject<List<MSTWord>>()
                            .OrderBy(w => w.Offset)
                            .ToList();

                            if (e.Result.Text == "" && wordLevelCaptions.Count == 0)
                            {

                                if (verboseLogging)
                                {
                                    TimeSpan _offset = new TimeSpan(e.Result.OffsetInTicks);
                                    TimeSpan _end = e.Result.Duration.Add(_offset);
                                    _logger.LogInformation($"{logId}: Empty String: Begin={_offset.Minutes}:{_offset.Seconds},{_offset.Milliseconds}, End={_end.Minutes}:{_end.Seconds},{_end.Milliseconds}");
                                }
                                return;
                            }

                            if (wordLevelCaptions.Any())
                            {

                                // TODO/TOREVIEW: Is this a bug fix or redefinition? Could this change in later versions of the SDK?
                                long offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                                
                                wordLevelCaptions.ForEach(w => w.Offset += offsetDifference);
                            }

                            var sentenceLevelCaptions = MSTWord.WordLevelTimingsToSentenceLevelTimings(e.Result.Text, wordLevelCaptions);

                            // Convert back to time in original untrimmed video.
                            // These timings are used to check if we should be adding any captions
                            // However they are then used direcly for sentence level translations
                            // but not for the word-level timings of the primary language
                            TimeSpan begin = (new TimeSpan(e.Result.OffsetInTicks)).Add(restartOffset);
                            TimeSpan end = e.Result.Duration.Add(begin);

                            if (verboseLogging)
                            {
                                _logger.LogInformation($"{logId}: Begin={begin.Minutes}:{begin.Seconds}.{begin.Milliseconds}", begin);
                                _logger.LogInformation($"{logId}: End={end.Minutes}:{end.Seconds}.{end.Milliseconds}");
                            }
                            // TODO/TOREVIEW:
                            // ToCaptionEntitiesWithWordTiming vs ToCaptionEntitiesInterpolate
                            // Can this code be simplified to use a single function?
                            // Also: Caution - it is possible that word timing data from MS may depend on SDK version

                            var newCaptions = MSTWord.ToCaptionEntitiesWithWordTiming(captions[sourceLanguage].Count,restartOffset, sentenceLevelCaptions);

                            if (begin >= startAfterMap[sourceLanguage])
                            {
                                captions[sourceLanguage].AddRange(newCaptions);
                                if (logOnce.Add("AddingMain"))
                                {
                                    _logger.LogInformation($"{logId}: Adding Primary Language captions");
                                }
                            }
                            else
                            {
                                if (logOnce.Add("SkippingMain"))
                                {
                                    _logger.LogInformation($"{logId}: Skipping Main captions because {begin} < {startAfterMap[sourceLanguage]}");
                                }
                            }
                            foreach (var element in e.Result.Translations)
                            {
                                var language = element.Key;
                                var startAfter = startAfterMap[language];
                                if (begin >= startAfter)
                                {
                                    // Translations dont have word level timing so
                                    // interpolate between start and end
                                    newCaptions = Caption.ToCaptionEntitiesInterpolate(captions[language].Count, begin, end, element.Value);
                                    captions[element.Key].AddRange(newCaptions);

                                    if (logOnce.Add($"AddingTranslated {language}"))
                                    {
                                        _logger.LogInformation($"{logId}: Adding translation ({language}) captions");
                                    }
                                }
                                else
                                {
                                    if (logOnce.Add($"SkippingTranslated {language}"))
                                    {
                                        _logger.LogInformation($"{logId}: Skipping ({language}) captions because {begin} < {startAfter}");
                                    }
                                }
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                             TimeSpan begin = (new TimeSpan(e.Result.OffsetInTicks)).Add(restartOffset);
                            _logger.LogInformation($"{logId}: NOMATCH: ({begin.Minutes}:{begin.Seconds}) Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        errorCode = e.ErrorCode.ToString();
                        _logger.LogInformation($"{logId}: CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {

                            _logger.LogError($"{logId}: CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                            if (e.ErrorCode == CancellationErrorCode.ServiceTimeout
                            || e.ErrorCode == CancellationErrorCode.ServiceUnavailable
                            || e.ErrorCode == CancellationErrorCode.ConnectionFailure)
                            {
                                TimeSpan lastTime = TimeSpan.Zero;
                                if (captions.Count != 0)
                                {
                                    var lastCaption = captions[sourceLanguage].OrderBy(c => c.End).TakeLast(1).ToList().First();
                                    lastTime = lastCaption.End;
                                }

                                _logger.LogInformation($"{logId}: Retrying, LastSuccessTime={lastTime}");
                                lastSuccessfulTime = lastTime;
                            }
                            else if (e.ErrorCode != CancellationErrorCode.NoError)
                            {
                                _logger.LogInformation($"{logId}: CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");
                                _slackLogger.PostErrorAsync(new Exception($"{logId}: Transcription Failure"),
                                    "Transcription Failure").GetAwaiter().GetResult();
                            }
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        _logger.LogInformation($"{logId}: Session started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        _logger.LogInformation($"{logId}: Session stopped event. Stopping recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                    _logger.LogInformation($"{logId}: Returning {captions.Count} languages, ErrorCode = {errorCode}, LastSuccessTime = {lastSuccessfulTime}");

                    return new MSTResult
                    {
                        Captions = captions,
                        ErrorCode = errorCode,
                        LastSuccessTime = lastSuccessfulTime
                    };
                }
            }
        }

        private SpeechTranslationConfig createSpeechTranslationConfig(String logId, Key key, string sourceLanguage, List<string> languages)
        {
            SpeechTranslationConfig speechConfig = SpeechTranslationConfig.FromSubscription(key.ApiKey, key.Region);
            speechConfig.RequestWordLevelTimestamps();
            if (!IsSupportedRecognition(sourceLanguage))
            {
                _logger.LogError($"{logId}: !!!! Unknown recognition language ({sourceLanguage})! Recogition may fail ...");
            }
            speechConfig.SpeechRecognitionLanguage = sourceLanguage;

            _logger.LogInformation($"{logId}: Requested output languages: { String.Join(",", languages) }, source = ({sourceLanguage})");
            String shortCodeSource = sourceLanguage.Split('-')[0].ToLower();
            foreach (var language in languages)
            {
                String shortCodeTarget = language.Split('-')[0].ToLower();
                if (shortCodeSource == shortCodeTarget)
                {
                    continue;
                }
                if (IsSupportedTranslation(language))
                {
                    _logger.LogInformation($"{logId}: Adding target {language}");
                    speechConfig.AddTargetLanguage(language);
                }
                else
                {
                    _logger.LogWarning($"{logId}: Skipping unsupported target {language}");
                }
            }



            speechConfig.OutputFormat = OutputFormat.Detailed;
            return speechConfig;
        }

        private async Task<CTGrpc.File> createTrimmedAudioFileAsync(string videoFilePath, float seconds)
        {
            var request = new CTGrpc.FileForConversion
            {
                File = new CTGrpc.File { FilePath = videoFilePath },
                Offset = seconds
            };
            return await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(request);
        }
    }
}
