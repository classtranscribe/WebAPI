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

namespace CTCommons.MSTranscription
{
    public class MSTranscriptionService
    {
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

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, FileRecord videoFile, Key key, Dictionary<string, List<Caption>> captions, TimeSpan offset)
        {
            return await RecognitionWithVideoStreamAsync(logId, videoFile.VMPath, key, captions, offset);
        }

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string logId, string videoFilePath, Key key, Dictionary<string, List<Caption>> captions, TimeSpan restartOffset)
        {
            _logger.LogInformation($"Trimming video file with offset {restartOffset.TotalSeconds} seconds");
            // If we ever re-use the audio file, we should remove the File.Delete at the end of this method
            var trimmedAudioFile = await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(new CTGrpc.FileForConversion
            {
                File = new CTGrpc.File { FilePath = videoFilePath },
                Offset = (float)restartOffset.TotalSeconds
            });

            string audioWavFilePath = trimmedAudioFile.FilePath;
            try
            {
                AppSettings _appSettings = Globals.appSettings;

                SpeechTranslationConfig _speechConfig = SpeechTranslationConfig.FromSubscription(key.ApiKey, key.Region);
                _speechConfig.RequestWordLevelTimestamps();
                // Sets source and target languages.
                _speechConfig.SpeechRecognitionLanguage = Languages.ENGLISH;
                _speechConfig.AddTargetLanguage(Languages.SIMPLIFIED_CHINESE);
                _speechConfig.AddTargetLanguage(Languages.KOREAN);
                _speechConfig.AddTargetLanguage(Languages.SPANISH);
                _speechConfig.AddTargetLanguage(Languages.FRENCH);
                _speechConfig.OutputFormat = OutputFormat.Detailed;

                TimeSpan lastSuccessfulTime = TimeSpan.Zero;
                string errorCode = "";

                //TODO: ADD Environment variable support
                bool verboseLogging = false;

                // TODO/TOREVIEW: Global change to Console!?
                Console.OutputEncoding = Encoding.Unicode;

                var stopRecognition = new TaskCompletionSource<int>();
                // Create an audio stream from a wav file.
                // Replace with your own audio file name.
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
                                var newCaptions = MSTWord.AppendCaptions(captions[Languages.ENGLISH].Count, sentenceLevelCaptions);


                            captions[Languages.ENGLISH].AddRange(newCaptions);

                                foreach (var element in e.Result.Translations)
                                {
                                    newCaptions = Caption.AppendCaptions(captions[element.Key].Count, offset, end, element.Value);
                                    captions[element.Key].AddRange(newCaptions);
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
                                        var lastCaption = captions[Languages.ENGLISH].OrderBy(c => c.End).TakeLast(1).ToList().First();
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
                    File.Delete(audioWavFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deleting {0}", audioWavFilePath);
                    // do not rethrow
                }
            }
            // </recognitionAudioStream>
        }
    }
}
