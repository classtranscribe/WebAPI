using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskEngine.MSTranscription
{
    public class MSTranscriptionService
    {
        public static class TranslationLanguages
        {
            public static string ENGLISH = "en-US";
            public static string SIMPLIFIED_CHINESE = "zh-Hans";
            public static string KOREAN = "ko";
            public static string SPANISH = "es";
        }
        public async Task<Tuple<Dictionary<string, List<Caption>>,string>> RecognitionWithAudioStreamAsync(string file)
        {
            AppSettings _appSettings = Globals.appSettings;
            
            Key key = TaskEngineGlobals.KeyProvider.GetKey();
            SpeechTranslationConfig _speechConfig = SpeechTranslationConfig.FromSubscription(key.ApiKey, key.Region);
            _speechConfig.RequestWordLevelTimestamps();
            // Sets source and target languages.
            _speechConfig.SpeechRecognitionLanguage = TranslationLanguages.ENGLISH;
            _speechConfig.AddTargetLanguage(TranslationLanguages.SIMPLIFIED_CHINESE);
            _speechConfig.AddTargetLanguage(TranslationLanguages.KOREAN);
            _speechConfig.AddTargetLanguage(TranslationLanguages.SPANISH);
            _speechConfig.OutputFormat = OutputFormat.Detailed;

            string errorCode = "";
            Console.OutputEncoding = Encoding.Unicode;
            Dictionary<string, List<Caption>> captions = new Dictionary<string, List<Caption>>();

            captions.Add(TranslationLanguages.ENGLISH, new List<Caption>());
            captions.Add(TranslationLanguages.SIMPLIFIED_CHINESE, new List<Caption>());
            captions.Add(TranslationLanguages.KOREAN, new List<Caption>());
            captions.Add(TranslationLanguages.SPANISH, new List<Caption>());


            var stopRecognition = new TaskCompletionSource<int>();
            bool fileWritten = false;
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
                            var words = jObject["Words"]
                            .ToObject<List<MSTWord>>()
                            .OrderBy(w => w.Offset)
                            .ToList();

                            // To fix bug with MS Cognitive Services
                            if (words.Any())
                            {
                                var offsetDifference = e.Result.OffsetInTicks - words.FirstOrDefault().Offset;
                                words.ForEach(w => w.Offset += offsetDifference);
                            }


                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            Console.Write($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            TimeSpan end = e.Result.Duration.Add(offset);
                            Console.WriteLine($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            Caption.AppendCaptions(captions[TranslationLanguages.ENGLISH], words);
                            
                            foreach (var element in e.Result.Translations)
                            {
                                Caption.AppendCaptions(captions[element.Key], offset, end, element.Value);                                
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        errorCode = e.ErrorCode.ToString();
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        }
                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nSession stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                        fileWritten = true;
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    TaskEngineGlobals.KeyProvider.ReleaseKey(key);
                    return new Tuple<Dictionary<string, List<Caption>>, string>(captions, errorCode);
                }
            }
            // </recognitionAudioStream>
        }
    }
}
