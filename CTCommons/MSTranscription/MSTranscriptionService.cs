using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons.Grpc;
using System.IO;


//TODO: Cognitive services provides a list of languages that are available for detection and for translation
// Auto language detection is also possibe
// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-languages
// e.g. 
// https://api.cognitive.microsofttranslator.com/languages?api-version=3.0
// Speech recognition list is here - 
// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#speech-translation
namespace CTCommons.MSTranscription
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

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, FileRecord videoFile, Key key, Dictionary<string, List<Caption>> captions, string sourceLanguage, Dictionary<string, TimeSpan> startAfterMap)
        {
            return await RecognitionWithVideoStreamAsync(logId, videoFile.VMPath, key, captions, sourceLanguage, startAfterMap);
        }
        /// <summary>
        /// Returns true if Cognitive Services supports this language code for recognition. Must match exactly in correct case e.g. en-US
        /// Not "en" or "en-us"
        /// </summary>
        /// <param name="dialect"></param>
        /// <returns>true if is this a valid language code for recognition</returns>
        private bool IsSupportedRecognition(string dialect)
        {
            return _supportedRecognition.Contains(dialect);
        }
        private bool IsSupportedTranslation(string language)
        {
            return _supportedTranslations.Contains(language);
        }

        //TODO/TOREVIEW: Refactor this method into setup/process/completion 
        // It is too long

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, string videoFilePath, Key key, Dictionary<string, List<Caption>> captions, string sourceLanguage, Dictionary<string, TimeSpan> startAfterMap)
        {
            TimeSpan restartOffset = TimeSpan.Zero;
            if (startAfterMap.Any())
                restartOffset = startAfterMap.Values.Min();

            _logger.LogInformation($"Trimming video file with offset {restartOffset.TotalSeconds} seconds");
            // If we ever re-use the audio file, we should remove the File.Delete at the end of this method
            var trimmedAudioFile = await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(new CTGrpc.FileForConversion
            {
                File = new CTGrpc.File { FilePath = videoFilePath },
                Offset = (float)restartOffset.TotalSeconds
            });

            string audioWavFilePath = trimmedAudioFile.FilePath; // Also used in finally block to delete the file
            try
            {
                
                SpeechTranslationConfig _speechConfig = SpeechTranslationConfig.FromSubscription(key.ApiKey, key.Region);
                _speechConfig.RequestWordLevelTimestamps();
                if (! IsSupportedRecognition(sourceLanguage))
                {
                    _logger.LogError($"{logId}: !!!! Unknown Source Language ({sourceLanguage})! Recogition may fail ...");
                }
                _speechConfig.SpeechRecognitionLanguage = sourceLanguage;

                _logger.LogInformation($"{logId}: Requested Target Languages: { String.Join(",", startAfterMap.Keys) }, source = ({sourceLanguage})");
                String shortCodeSource = sourceLanguage.Split('-')[0].ToLower();
                foreach (var language in startAfterMap.Keys)
                {
                    String shortCodeTarget = language.Split('-')[0].ToLower();
                    if (shortCodeSource == shortCodeTarget)
                    {
                        continue;
                    }
                    if (IsSupportedTranslation(language))
                    {
                        _logger.LogInformation($"{logId}: Adding Target {language}");
                        _speechConfig.AddTargetLanguage(language);
                    }
                    else
                    {
                        _logger.LogWarning($"{logId}: Skipping unsupported target {language}");
                    }
                }

                // Potential Gotchas to consider: The set of languages may be different than previous attempts

                _speechConfig.OutputFormat = OutputFormat.Detailed;

                TimeSpan lastSuccessfulTime = TimeSpan.Zero;
                string errorCode = "";

                //TODO: ADD Environment variable support
                bool verboseLogging = false;

                var stopRecognition = new TaskCompletionSource<int>();

                using (var audioInput = WavHelper.OpenWavFile(audioWavFilePath))
                {

                    // Creates a speech recognizer using audio stream input.
                    using (var recognizer = new TranslationRecognizer(_speechConfig, audioInput))
                    {
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
                                    var offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                                    wordLevelCaptions.ForEach(w => w.Offset += offsetDifference);
                                }

                                var sentenceLevelCaptions = MSTWord.WordLevelTimingsToSentenceLevelTimings(e.Result.Text, wordLevelCaptions);

                                TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);

                                offset.Add(restartOffset);

                                TimeSpan end = e.Result.Duration.Add(offset);
                                if (verboseLogging)
                                {
                                    _logger.LogInformation($"{logId}: Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                                    _logger.LogInformation($"{logId}: End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                                }
                                var newCaptions = MSTWord.AppendCaptions(captions[sourceLanguage].Count, sentenceLevelCaptions);

                                if (offset >= startAfterMap[sourceLanguage])
                                {
                                    captions[sourceLanguage].AddRange(newCaptions);
                                }
                                else
                                {
                                    _logger.LogInformation($"{logId}: Skipping Main captions because {offset} < {startAfterMap[sourceLanguage]}");
                                }
                                foreach (var element in e.Result.Translations)
                                {
                                    if (offset >= startAfterMap[element.Key])
                                    {
                                        newCaptions = Caption.AppendCaptions(captions[element.Key].Count, offset, end, element.Value);
                                        captions[element.Key].AddRange(newCaptions);
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"{logId}: Skipping {element.Key} captions because {offset} < {startAfterMap[element.Key]}");
                                    }
                                }
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                _logger.LogInformation($"{logId}: NOMATCH: Speech could not be recognized.");
                            }
                        };

                        recognizer.Canceled += (s, e) =>
                        {
                            errorCode = e.ErrorCode.ToString();
                            _logger.LogInformation($"{logId}: CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                _logger.LogInformation($"{logId}: CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");

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

                                    _logger.LogInformation($"{logId}: Retrying, LastSuccessTime={lastTime.ToString()}");
                                    lastSuccessfulTime = lastTime;
                                }
                                else if (e.ErrorCode != CancellationErrorCode.NoError)
                                {
                                    _logger.LogInformation($"{logId}: CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
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

                        _logger.LogInformation($"{logId}: Returning {captions.Count} captions, ErrorCode = {errorCode}, LastSuccessTime = {lastSuccessfulTime}");

                        return new MSTResult
                        {
                            Captions = captions,
                            ErrorCode = errorCode,
                            LastSuccessTime = lastSuccessfulTime
                        };
                    }
                }
            }
            finally
            {
                try
                {
                    if(File.Exists(audioWavFilePath)){
                        File.Delete(audioWavFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deleting {0}", audioWavFilePath);
                    // do not rethrow
                }
            }
            // 
        }
    }
}
