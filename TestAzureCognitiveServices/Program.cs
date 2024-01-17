using System;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace TestAzureCognitiveServices
{
    public class WavHelper
    {
        public static AudioConfig OpenWavFile(string filename)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(filename));
            return OpenWavFile(reader);
        }

        public static AudioConfig OpenWavFile(BinaryReader reader)
        {
            // Tag "RIFF"
            char[] data = new char[4];
            reader.Read(data, 0, 4);
            Trace.Assert((data[0] == 'R') && (data[1] == 'I') && (data[2] == 'F') && (data[3] == 'F'), "Wrong wav header");

            // Chunk size
            long fileSize = reader.ReadInt32();

            // Subchunk, Wave Header
            // Subchunk, Format
            // Tag: "WAVE"
            reader.Read(data, 0, 4);
            Trace.Assert((data[0] == 'W') && (data[1] == 'A') && (data[2] == 'V') && (data[3] == 'E'), "Wrong wav tag in wav header");

            // Tag: "fmt"
            reader.Read(data, 0, 4);
            Trace.Assert((data[0] == 'f') && (data[1] == 'm') && (data[2] == 't') && (data[3] == ' '), "Wrong format tag in wav header");

            // chunk format size
            var formatSize = reader.ReadInt32();
            var formatTag = reader.ReadUInt16();
            var channels = reader.ReadUInt16();
            var samplesPerSecond = reader.ReadUInt32();
            var avgBytesPerSec = reader.ReadUInt32();
            var blockAlign = reader.ReadUInt16();
            var bitsPerSample = reader.ReadUInt16();

            // Until now we have read 16 bytes in format, the rest is cbSize and is ignored for now.
            if (formatSize > 16)
                reader.ReadBytes((int)(formatSize - 16));

            // Second Chunk, data
            // tag: data.
            reader.Read(data, 0, 4);
            // Trace.Assert((data[0] == 'd') && (data[1] == 'a') && (data[2] == 't') && (data[3] == 'a'), "Wrong data tag in wav");
            // data chunk size
            int dataSize = reader.ReadInt32();

            // now, we have the format in the format parameter and the
            // reader set to the start of the body, i.e., the raw sample data
            AudioStreamFormat format = AudioStreamFormat.GetWaveFormatPCM(samplesPerSecond, (byte)bitsPerSample, (byte)channels);
            return AudioConfig.FromStreamInput(new BinaryAudioStreamReader(reader), format);
        }
    }

    /// <summary>
    /// Adapter class to the native stream api.
    /// </summary>
    public sealed class BinaryAudioStreamReader : PullAudioInputStreamCallback
    {
        private System.IO.BinaryReader _reader;

        /// <summary>
        /// Creates and initializes an instance of BinaryAudioStreamReader.
        /// </summary>
        /// <param name="reader">The underlying stream to read the audio data from. Note: The stream contains the bare sample data, not the container (like wave header data, etc).</param>
        public BinaryAudioStreamReader(System.IO.BinaryReader reader)
        {
            _reader = reader;
        }

        /// <summary>
        /// Creates and initializes an instance of BinaryAudioStreamReader.
        /// </summary>
        /// <param name="stream">The underlying stream to read the audio data from. Note: The stream contains the bare sample data, not the container (like wave header data, etc).</param>
        public BinaryAudioStreamReader(System.IO.Stream stream)
            : this(new System.IO.BinaryReader(stream))
        {
        }

        /// <summary>
        /// Reads binary data from the stream.
        /// </summary>
        /// <param name="dataBuffer">The buffer to fill</param>
        /// <param name="size">The size of the buffer.</param>
        /// <returns>The number of bytes filled, or 0 in case the stream hits its end and there is no more data available.
        /// If there is no data immediate available, Read() blocks until the next data becomes available.</returns>
        public override int Read(byte[] dataBuffer, uint size)
        {
            return _reader.Read(dataBuffer, 0, (int)size);
        }

        /// <summary>
        /// This method performs cleanup of resources.
        /// The Boolean parameter <paramref name="disposing"/> indicates whether the method is called from <see cref="IDisposable.Dispose"/> (if <paramref name="disposing"/> is true) or from the finalizer (if <paramref name="disposing"/> is false).
        /// Derived classes should override this method to dispose resource if needed.
        /// </summary>
        /// <param name="disposing">Flag to request disposal.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                _reader.Dispose();
            }

            disposed = true;
            base.Dispose(disposing);
        }

        private bool disposed = false;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            try
            {
                string result = useAzureTranslationAsync().GetAwaiter().GetResult();
                Console.WriteLine(result);
            } catch(Exception e)
            {
                Console.WriteLine(e);

            }
        }

        async static Task<string> useAzureTranslationAsync() {
            var defaultKeys = "996885bc424b4fda9df983c404e7309c,westus";
            Console.WriteLine($"Environment variable AZURE_SUBSCRIPTION_KEYS=key,region;...");
            var keys = System.Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_KEYS") ?? defaultKeys;
            
            var onekeyregion = keys.Split(";")[0].Split(",");
            var key = onekeyregion[0].Trim();
            var region = onekeyregion[1].Trim();

            Console.WriteLine($"Key {key.Substring(0, 3)}... Region {region}");

            // Production string: '-c:a pcm_s16le -ac 1 -y -ar 16000 -f wav
            // ffmpeg -y -i video-source.mp4 -acodec pcm_s16le -f s16le -ac 1 -ar 16000 audio.wav

            //var audioFile = "E:\\proj2\\Deployment\\WebAPI\\TestAzureCognitiveServices\\shortwav.wav";
            var audioFile = "/shortwav.wav";


            SpeechTranslationConfig speechConfig = SpeechTranslationConfig.FromSubscription(key, region);
            speechConfig.RequestWordLevelTimestamps();
            speechConfig.SetProperty(PropertyId.Speech_LogFilename, "Logfile.txt");

            speechConfig.SpeechRecognitionLanguage = "en-US";

            speechConfig.AddTargetLanguage("zh-Hans");
            speechConfig.AddTargetLanguage("ko");
            speechConfig.AddTargetLanguage("es");
            speechConfig.AddTargetLanguage("fr");
            

            using (var audioInput = WavHelper.OpenWavFile(audioFile))
            {
                var stopRecognition = new TaskCompletionSource<int>();
                using (var recognizer = new TranslationRecognizer(speechConfig, audioInput))
                {
                    var grammar = PhraseListGrammar.FromRecognizer(recognizer);
                    grammar.AddPhrase("byte");

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {

                            Console.WriteLine(e.Result.Text);

                            foreach (var element in e.Result.Translations)
                            {
                                Console.WriteLine($"Adding translation ({element}) captions");
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        var errorCode = e.ErrorCode.ToString();
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {

                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                            if (e.ErrorCode == CancellationErrorCode.ServiceTimeout
                            || e.ErrorCode == CancellationErrorCode.ServiceUnavailable
                            || e.ErrorCode == CancellationErrorCode.ConnectionFailure)
                            {


                            }
                            else if (e.ErrorCode != CancellationErrorCode.NoError)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");
                            }
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("SessionStarted");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("SessionStopped");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    return "done";
                }
            }
        }
    }
}
