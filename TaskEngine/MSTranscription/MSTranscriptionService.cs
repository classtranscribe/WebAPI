using ClassTranscribeDatabase;
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
        public async Task<Tuple<Dictionary<string, List<Sub>>, Dictionary<string, string>, string>> RecognitionWithAudioStreamAsync(string file)
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
            Dictionary<string, List<Sub>> captions = new Dictionary<string, List<Sub>>();
            Dictionary<string, string> vttFiles = new Dictionary<string, string>();

            captions.Add(TranslationLanguages.ENGLISH, new List<Sub>());
            captions.Add(TranslationLanguages.SIMPLIFIED_CHINESE, new List<Sub>());
            captions.Add(TranslationLanguages.KOREAN, new List<Sub>());
            captions.Add(TranslationLanguages.SPANISH, new List<Sub>());


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

                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            TimeSpan end = e.Result.Duration.Add(offset);
                            Console.WriteLine($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            List<Sub> englishSubs = Sub.GetSubs(words);
                            englishSubs.ForEach(s => Console.WriteLine(s.Caption));
                            captions[TranslationLanguages.ENGLISH].AddRange(englishSubs);

                            foreach (var element in e.Result.Translations)
                            {
                                List<Sub> subs = Sub.GetSubs(new Sub
                                {
                                    Begin = offset,
                                    End = end,
                                    Caption = element.Value
                                });
                                captions[element.Key].AddRange(subs);
                                // subs.ForEach(s => Console.WriteLine(s.Caption));
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
                        if (!fileWritten)
                        {
                            foreach (var language in captions.Keys)
                            {
                                string vttFile = Sub.GenerateWebVTTFile(captions[language], file, language);
                                vttFiles.Add(language, vttFile);
                            }
                        }
                        fileWritten = true;
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
                        foreach (var language in captions.Keys)
                        {
                            string vttFile = Sub.GenerateWebVTTFile(captions[language], file, language);
                            if (!vttFiles.ContainsKey(language))
                            {
                                vttFiles.Add(language, vttFile);
                            }
                        }
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
                    return new Tuple<Dictionary<string, List<Sub>>, Dictionary<string, string>, string>(captions, vttFiles, errorCode);
                }
            }
            // </recognitionAudioStream>
        }
    }
}
