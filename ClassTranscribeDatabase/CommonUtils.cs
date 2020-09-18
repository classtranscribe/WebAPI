using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassTranscribeDatabase
{
    public class CommonUtils
    {
        // Explicitly number entries because these will exist in the database
        // and we dont want the meaning of existing entries to change due to future modifications of this list
        public enum TaskType
        {
            PeriodicCheck =1,
            DownloadAllPlaylists=2,
            DownloadPlaylistInfo=3,
            DownloadMedia=4,
            ConvertMedia=5,
            Transcribe=6,
            ProcessVideo=7,
            Aggregator=8,
            GenerateVTTFile=9,
            QueueAwaker=10,
            SceneDetection=11,
            UpdateBoxToken=12,
            CreateBoxToken=13,
            UpdateOffering=14,
            ReTranscribePlaylist=15
        }

        public class Languages
        {
            public static string ENGLISH = "en-US";
            public static string SIMPLIFIED_CHINESE = "zh-Hans";
            public static string KOREAN = "ko";
            public static string SPANISH = "es";
            public static string FRENCH = "fr";
        }

        public static string BOX_ACCESS_TOKEN = "BOX_ACCESS_TOKEN";
        public static string BOX_REFRESH_TOKEN = "BOX_REFRESH_TOKEN";
        public static byte[] MessageToBytes<T>(T obj)
        {
            string output = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(output);
        }

        public static T BytesToMessage<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        private static int seed; // member var only for future debugging /replay options
        private static Random random;
        /// <summary>Ensures we have a well seeded RNG even if two processes or two threads  are started at the same time</summary>    
        private static Random getSharedRandom()
        {
            if (random != null) 
                return random; // Early return
            // Use something external and trust worthy if it exists
            const string randomStream = "/dev/urandom";
            if (File.Exists(randomStream)) {
                using (BinaryReader reader = new BinaryReader(File.Open(randomStream, FileMode.Open)))
                {
                    seed = reader.ReadInt32();
                }
            } else
            {
                // Fallback
                seed = Guid.NewGuid().ToString().GetHashCode();
            }
            // TODO/REVIEW:Random ctor seed is limited to 32 bits!
            // Eventually we will need something better
            random = new Random(seed);
            return random;
        }
        public static string RandomString(int length)
        {
            // TODO/TOREVIEW: C# random is seeded with a time since 1970 at 100ns, so what if two
            // threads start at the same time, to within the tick resolution?
            // e.g. https://stackoverflow.com/questions/1785744/how-do-i-seed-a-random-class-to-avoid-getting-duplicate-random-values#
            // Could this mean that two threads cmight be seeded with the same number
            Random random = getSharedRandom();
            // Some filesystems are case insensitive so only use uppercase
            // Drop 1 and 0 because they are too similar to IO
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";
            string result;
            lock (random)
            {
                 result = new string(Enumerable.Repeat(chars, length)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            return result;
        }

        public static string GetTmpFile()
        {
            // Align with python code.
            const int filenaNmeLength = 12; // was 8
            while(true) {
                string candidate = Path.Combine(Globals.appSettings.DATA_DIRECTORY, RandomString(filenaNmeLength));
                if( ! File.Exists(candidate)) return candidate;
            }   
        }
    }
}
