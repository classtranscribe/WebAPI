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

namespace CTCommons.MSTranscription
{
    public class MSTranscriptionService
    {
        readonly ILogger _logger;
        readonly SlackLogger _slackLogger;

        public MSTranscriptionService(ILogger<MSTranscriptionService> logger, SlackLogger slackLogger)
        {
            _logger = logger;
            _slackLogger = slackLogger;
        }
        public async Task<Tuple<Dictionary<string, List<Caption>>, string>> RecognitionWithAudioStreamAsync(FileRecord audioFile, Key key)
        {
            string file = audioFile.Path;
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

            string errorCode = "";
            Console.OutputEncoding = Encoding.Unicode;
            Dictionary<string, List<Caption>> captions = new Dictionary<string, List<Caption>>
            {
                { Languages.ENGLISH, new List<Caption>() },
                { Languages.SIMPLIFIED_CHINESE, new List<Caption>() },
                { Languages.KOREAN, new List<Caption>() },
                { Languages.SPANISH, new List<Caption>() },
                { Languages.FRENCH, new List<Caption>() }
            };

            
            var stopRecognition = new TaskCompletionSource<int>();
            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (var audioInput = Helper.OpenWavFile(file))
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

                            if (wordLevelCaptions.Any())
                            {
                                var offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                                wordLevelCaptions.ForEach(w => w.Offset += offsetDifference);
                            }

                            var sentenceLevelCaptions = MSTWord.WordLevelTimingsToSentenceLevelTimings(e.Result.Text, wordLevelCaptions);

                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            _logger.LogInformation($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            TimeSpan end = e.Result.Duration.Add(offset);
                            _logger.LogInformation($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            Caption.AppendCaptions(captions[Languages.ENGLISH], sentenceLevelCaptions);

                            foreach (var element in e.Result.Translations)
                            {
                                Caption.AppendCaptions(captions[element.Key], offset, end, element.Value);
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            _logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        errorCode = e.ErrorCode.ToString();
                        _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
                            if (e.ErrorCode == CancellationErrorCode.AuthenticationFailure)
                            {
                                _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
                                _slackLogger.PostErrorAsync(new Exception($"Transcription Failure, Authentication failure, VideoId {audioFile.Id}"),
                                    $"Transcription Failure, Authentication failure, VideoId {audioFile.Id}").GetAwaiter().GetResult();
                            }
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        _logger.LogInformation("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        _logger.LogInformation("\nSession stopped event.");
                        _logger.LogInformation("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    return new Tuple<Dictionary<string, List<Caption>>, string>(captions, errorCode);
                }
            }
            // </recognitionAudioStream>
        }
    }
}
