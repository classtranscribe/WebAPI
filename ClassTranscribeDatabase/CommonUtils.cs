using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassTranscribeDatabase
{
    public class CommonUtils
    {
        public enum TaskType
        {
            PeriodicCheck,
            DownloadAllPlaylists,
            DownloadPlaylistInfo,
            DownloadMedia,
            ConvertMedia,
            Transcribe,
            ProcessVideo,
            Aggregator,
            GenerateVTTFile,
            QueueAwaker,
            SceneDetection,
            UpdateBoxToken,
            CreateBoxToken,
            UpdateOffering
        }

        public class Languages
        {
            public static string ENGLISH = "en-US";
            public static string SIMPLIFIED_CHINESE = "zh-Hans";
            public static string KOREAN = "ko";
            public static string SPANISH = "es";
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

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetTmpFile()
        {
            return Path.Combine(Globals.appSettings.DATA_DIRECTORY, RandomString(8));
        }
    }
}
