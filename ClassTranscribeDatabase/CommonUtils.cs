﻿using ClassTranscribeDatabase.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
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
            TranscribeVideo = 6,
            ProcessVideo = 7,
            Aggregator = 8,
            GenerateVTTFile = 9,
            QueueAwaker = 10,
            SceneDetection = 11,
            UpdateBoxToken = 12,
            CreateBoxToken = 13,
            UpdateOffering = 14,
            ReTranscribePlaylist = 15,
            BuildElasticIndex = 16,
            ExampleTask = 17,
            CleanUpElasticIndex = 18,
            PythonCrawler = 19 
        }

        public class Languages
        {
            //  Speech recognition uses a dialect
            public static string ENGLISH_AMERICAN = "en-US";
            
            // Translations use just a short language code
            public static string SIMPLIFIED_CHINESE = "zh-Hans";
            public static string KOREAN = "ko";
            public static string SPANISH = "es";
            public static string FRENCH = "fr";
            // See MSTRanscriptionTask for a full list of recognition and translation languages
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
            // Drop aeiouy to prevent unwanted words; drop 1 and 0 because they are too similar to IO
            const string chars = "bcdfghjklmnpqrstvwxzBCDFGHJKLMNPQRSTVWXZ23456789";

            // This new GUID-as-source of random bytes implementation avoids an exclusive file lock in /dev/urandom
            // ... And also overcomes the 2^32 seed limitation of the random class (4bn states is insufficient if we have a million files)
            // ... And gives me an excuse to implement a  simple fair random number generator.

            // To understand the next line, imagine chars.length was 100. We would only accept the values 0 - 199
            int maxFair = 256 - (256 % chars.Length);

            StringBuilder result = new StringBuilder(length);
            while (true)
            {
                foreach (byte b in Guid.NewGuid().ToByteArray()) // 16 bytes of chaos
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
            const int filenameLength = 12; // was 8
            while (true)
            {
                string candidate = Path.Combine(Globals.appSettings.DATA_DIRECTORY, RandomString(filenameLength));
                if (!File.Exists(candidate)) return candidate;
            }
        }

        public static string GetMediaName(Media media)
        {
            string name;

            switch (media.SourceType)
            {
                case SourceType.Echo360:
                    name = media.JsonMetadata["title"]?.ToString();

                    if (string.IsNullOrEmpty(name))
                    {
                        string lessonName = media.JsonMetadata["lessonName"]?.ToString() ?? "Untitled";
                        DateTime createdAt = Convert.ToDateTime(
                            media.JsonMetadata["createdAt"]?.ToString() ?? "01/01/1970",
                            CultureInfo.InvariantCulture);

                        name = $"{lessonName} {createdAt.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)}";
                    }

                    break;

                case SourceType.Youtube:
                    var title = media.JsonMetadata["title"]?.ToString();
                    name = string.IsNullOrEmpty(title) ? "Untitled" : title;
                    break;

                case SourceType.Local:
                    var fileName = media.JsonMetadata["filename"]?.ToString();

                    if (string.IsNullOrEmpty(fileName) && media.JsonMetadata.ContainsKey("video1")) {
                        fileName = JObject.Parse(media.JsonMetadata["video1"].ToString())?["FileName"]?.ToString();
                    }

                    fileName ??= "Untitled";
                    name = fileName.Replace(".mp4", "");
                    break;

                case SourceType.Kaltura:
                    var videoName = media.JsonMetadata["name"]?.ToString() ?? "Untitled";
                    var videoDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(media.JsonMetadata["createdAt"]?.ToObject<int>() ?? 0);
                    name = $"{videoName} {videoDate.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)}";
                    break;

                case SourceType.Box:
                    name = media.JsonMetadata["name"]?.ToString() ?? "Untitled";
                    break;

                default:
                    name = "Untitled";
                    break;
            }

            return name;
        }
        public static string ToCourseOfferingSubDirectory(CTDbContext ctx, Entity entity) {
            String? path = GetRelatedCourseOfferingFilePath(ctx, entity);
            
            if( !string.IsNullOrEmpty(path ) ) {
                return path;
            }
            return "/data/"; //legacy, pre 2022, default = everything is stored in the same directory
        }

        public static string? GetRelatedCourseOfferingFilePath(CTDbContext ctx, Entity entity)
        {
            // the only thing that we can trust exists on the given the entity Id
            // Drop recursion... this may reduce the number of SQL calls
            // Or maybe this needs to be rewritten - it is possible that each traversal 
            // is another lazy load of the next object.

           
            switch (entity)
            {
                case CourseOffering co:
                    return ctx.CourseOfferings.Where(co2 => co2.Id == co.Id).OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;
                    
                case Course c:
                    return ctx.CourseOfferings.Where(c2 => c2.CourseId == c.Id)
                        .OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;

                case Media m:
                    return ctx.Medias.FirstOrDefault(m2=>m2.Id == m.Id ).Playlist.Offering
                        .CourseOfferings.OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;

                case Offering o:
                    return ctx.CourseOfferings.Where(co2 => co2.OfferingId == o.Id)
                        .OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;

                case Playlist p:
                    return ctx.Playlists.FirstOrDefault(p2=>p2.Id == p.Id ).Offering
                        .CourseOfferings.OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;
  
                case Transcription t:
                    return ctx.Transcriptions.FirstOrDefault(t2=>t2.Id == t.Id)?.Video
                        .Medias.OrderBy(co=>co.CreatedAt).FirstOrDefault()
                        ?.Playlist.Offering
                        .CourseOfferings.OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;
                    
                case Video v:
                    return ctx.Medias.OrderBy(co=>co.CreatedAt).FirstOrDefault(m2=>m2.VideoId == v.Id )
                        .Playlist.Offering
                        .CourseOfferings.OrderBy(co=>co.CreatedAt).FirstOrDefault()?.FilePath;

                default:
                    throw new InvalidOperationException($"GetRelatedCourseOffering not implemented for type {entity.GetType()} (Object ID: {entity.Id})");
            }
        }
    }
}
