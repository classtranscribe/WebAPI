using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            TranscribeMedia,
            ProcessVideo,
            Aggregator,
            GenerateVTTFile,
            QueueAwaker,
            GenerateEPubFile
        }

        public class Languages
        {
            public static string ENGLISH = "en-US";
            public static string SIMPLIFIED_CHINESE = "zh-Hans";
            public static string KOREAN = "ko";
            public static string SPANISH = "es";
        }
        public static byte[] MessageToBytes<T>(T obj)
        {
            string output = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(output);
        }

        public static T BytesToMessage<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}
