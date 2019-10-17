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
            QueueAwaker
        }
        public static byte[] MessageToBytes<T>(T obj)
        {
            string output = JsonConvert.SerializeObject(obj);
            Console.WriteLine(" [x] Sending {0}", output);
            return Encoding.UTF8.GetBytes(output);
        }

        public static T BytesToMessage<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}
