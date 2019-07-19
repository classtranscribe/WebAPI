using ClassTranscribeDatabase;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskEngine.MSTranscription
{
    public class MSTranscriptionService
    {
        private AppSettings _appSettings;
        private SpeechConfig _speechConfig;
        public MSTranscriptionService()
        {
            _appSettings = Globals.appSettings;

            _speechConfig = SpeechConfig.FromSubscription(_appSettings.AZURE_SUBSCRIPTION_KEY, _appSettings.AZURE_REGION);
            _speechConfig.RequestWordLevelTimestamps();
            _speechConfig.OutputFormat = OutputFormat.Detailed;
        }
        public async Task<string> RecognitionWithAudioStreamAsync(string file)
        {
            // <recognitionAudioStream>
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            string vttFile = "";
            var stopRecognition = new TaskCompletionSource<int>();
            bool fileWritten = false;
            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (var audioInput = Helper.OpenWavFile(file))
            {
                // Creates a speech recognizer using audio stream input.
                using (var recognizer = new SpeechRecognizer(_speechConfig, audioInput))
                {
                    //// Subscribes to events.
                    //recognizer.Recognizing += (s, e) =>
                    //{
                    //    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                    //};

                    List<List<MSTWord>> allWords = new List<List<MSTWord>>();
                    List<Sub> subs = new List<Sub>();
                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            List<DetailedSpeechRecognitionResult> detailedSpeechRecognitionResults = e.Result.Best().ToList();
                            JObject jObject = JObject.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult));
                            var words = jObject["NBest"].OrderByDescending(o => o["Confidence"])
                            .Take(1)
                            .Select(o => o["Words"])
                            .First()
                            .ToObject<List<MSTWord>>()
                            .OrderBy(w => w.Offset)
                            .ToList();

                            allWords.Add(words);

                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            // Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}");
                            TimeSpan end = e.Result.Duration.Add(offset);
                            Console.WriteLine($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            // Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}");
                            subs.AddRange(Sub.GetSubs(words));
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        }
                        stopRecognition.TrySetResult(0);
                        if (!fileWritten)
                        {
                            Sub.GenerateSrtFile(subs, file);
                            vttFile = Sub.GenerateWebVTTFile(subs, file);
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
                        Sub.GenerateSrtFile(subs, file);
                        vttFile = Sub.GenerateWebVTTFile(subs, file);
                        fileWritten = true;
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    return vttFile;
                }
            }
            // </recognitionAudioStream>
        }
    }
}
