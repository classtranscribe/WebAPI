﻿using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RabbitMQ.Client.Content;
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
            PeriodicCheck = 1,
            DownloadAllPlaylists = 2,
            DownloadPlaylistInfo = 3,
            DownloadMedia = 4,
            ConvertMedia = 5,
            Transcribe = 6,
            ProcessVideo = 7,
            Aggregator = 8,
            GenerateVTTFile = 9,
            QueueAwaker = 10,
            SceneDetection = 11,
            UpdateBoxToken = 12,
            CreateBoxToken = 13,
            UpdateOffering = 14,
            ReTranscribePlaylist = 15
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

        public static string RandomString(int length)
        {
            // TODO/TOREVIEW: C# random is seeded with a time since 1970 at 100ns, so what if two
            // threads start at the same time, to within the tick resolution?
            // e.g. https://stackoverflow.com/questions/1785744/how-do-i-seed-a-random-class-to-avoid-getting-duplicate-random-values#
            // Could this mean that two threads cmight be seeded with the same number

            // Some filesystems are case insensitive so only use uppercase
            // Drop 1 and 0 because they are too similar to IO
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";

            // The day we want GUID-like filenames we can return Guid.NewGuid().ToString()
            // But for now there are too many GUIDS when debugging data, so let's keep filename as not GUID like

            // This new GUID as source of random bytes implementation avoids a race file lock in /dev/urandom
            // To understand the next line, imagine chars.length was 100. We would only want the values 0 - 199
            int maxFair = 255 - (255 % chars.Length);

            StringBuilder result = new StringBuilder(length);
            while (true)
            {
                foreach (byte b in Guid.NewGuid().ToByteArray())
                {
                    if (b < maxFair)
                    {
                        result.Append( chars[b % chars.Length]);
                        if (result.Length == length)
                        {
                            return result.ToString();
                        }
                    }

                }
            }
        }

        public static string GetTmpFile()
        {
            // Align with python code.
            const int filenaNmeLength = 12; // was 8
            while (true)
            {
                string candidate = Path.Combine(Globals.appSettings.DATA_DIRECTORY, RandomString(filenaNmeLength));
                if (!File.Exists(candidate)) return candidate;
            }
        }
    }
}
