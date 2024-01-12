using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Migrations;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using ClassTranscribeDatabase.Services.MSTranscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace DevExperiments
{
    class TempCode
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext context;
        private readonly MSTranscriptionService _transcriptionService;
        private readonly RpcClient _rpcClient;

        public TempCode(CTDbContext c, MSTranscriptionService transcriptionService, RpcClient rpcClient)
        {
            context = c;
            _transcriptionService = transcriptionService;
            _rpcClient = rpcClient;
        }

        public async Task ReadEntity()
        {
            Transcription transcription = context.Transcriptions.FirstOrDefault((t=>t.Id == "a3013ae4-869a-4de4-97d2-101af3ef75d7"));
            Video video = context.Videos.OrderByDescending(co => co.CreatedAt).FirstOrDefault();
            Playlist playlist = context.Playlists.OrderByDescending(co => co.CreatedAt).FirstOrDefault();
            CourseOffering co = context.CourseOfferings.OrderByDescending(co => co.CreatedAt).FirstOrDefault();
            Offering off = context.Offerings.OrderByDescending(co => co.CreatedAt).FirstOrDefault();
            Media media = context.Medias.OrderByDescending(co => co.CreatedAt).FirstOrDefault();

            List<Entity> items = new List<Entity>();
            items.Add(video);
            items.Add(playlist);
            items.Add(transcription);
            items.Add(co);
            items.Add(off);
            items.Add(media);

            foreach(var entity in items) {
                var filePath1 = CommonUtils.ToCourseOfferingSubDirectory(context, entity);
                Console.WriteLine(filePath1);
            }

        }
        public async Task ReadEntity2()
        {
            int count = await context.Transcriptions.CountAsync();
            Console.WriteLine($"Count: {count}");
            var tid1 = "4aa9e224-75f0-4775-a531-a5b0c99693f0"; // playlist deleted
            var tid2 = "a3013ae4-869a-4de4-97d2-101af3ef75d7";
            var tid3 = "a3013ae4-869a-4de4-97d2-101af3ef75d7"; // "8c4f01ec-5620-49e5-aa33-89cdfa37ac55"; // "4d47ee07-6bbc-47b7-8da5-6a80cf006f1c";
            var tid = tid1;
            var playlistId = (await context.Transcriptions.Include(t => t.Video).ThenInclude(v=>v.Medias).FirstOrDefaultAsync(t2 => t2.Id == tid)) 
                    ?.Video.Medias.OrderBy(co => co.CreatedAt).Select(m=>m.PlaylistId).FirstOrDefault();
            var offeringId =(await context.Playlists.Include(p => p.Offering).FirstOrDefaultAsync(p => p.Id == playlistId)) ?.Offering.Id;
            CourseOffering courseoff = (await context.CourseOfferings.Where(co => co.OfferingId == offeringId).OrderBy(co => co.CreatedAt).FirstOrDefaultAsync());
            string filepath = courseoff?.FilePath;

            Console.WriteLine($"# playlistId isnull {playlistId == null},{playlistId}");
            Console.WriteLine($"# Offering isnull {offeringId == null},{offeringId}");
            Console.WriteLine($"courseoff isnull:{courseoff == null}, {courseoff} ");
            Console.WriteLine($"FilePath isnull:{filepath == null}, {filepath} ");


            /*            string filepath = context.Transcriptions.FirstOrDefault(t2 => t2.Id == tid)?.Video
                                   .Medias.OrderBy(co => co.CreatedAt).FirstOrDefault()
                                   ?.Playlist.Offering
                                   .CourseOfferings.OrderBy(co => co.CreatedAt).FirstOrDefault()?.FilePath; */
            //  Console.WriteLine($"Path:{filepath}");
            return;
        }
        void ignore() { 
            // Transcription t1 = context.Transcriptions.Include("Video").FirstOrDefault(t2 => t2.Id == tid);

            //Console.WriteLine($"T found {t1.Id} -> {t1.VideoId}");
            //Video v2 = context.Videos.Find( t1.VideoId);
            //Console.WriteLine($"V2 found {v2.Id}");

            // Video v1 = t1.Video;

            // Console.WriteLine($"V1 found {v1.Id}");

            // List<Media> m1 = context.Transcriptions.FirstOrDefault(t2 => t2.Id == tid)?.Video.Medias;
            
            // Media m1 = context.Transcriptions.Include(t => t.Video.Medias).FirstOrDefault(t2 => t2.Id == tid)?.Video.Medias.OrderBy(co => co.CreatedAt).FirstOrDefault();
            // Console.WriteLine($"# Media found {m1}");

            // Offering o1 = context.Playlists.Include(p => p.Offering).FirstOrDefault(p => p.Id == m1.PlaylistId).Offering;
            // Console.WriteLine($"# Offering found {o1}");

            // var filepath = context.Playlists.Include(p=>p.Offering.CourseOfferings).FirstOrDefault(p => p.Id == m1.PlaylistId).Offering.CourseOfferings.OrderBy(co => co.CreatedAt).FirstOrDefault()?.FilePath;
            
            //Offering o1 = context.Transcriptions.FirstOrDefault(t2 => t2.Id == tid)?.Video
            //            .Medias.OrderBy(co => co.CreatedAt).FirstOrDefault()
            //            ?.Playlist.Offering;

            
        }
        public void Temp()
        {
            ReadEntity().GetAwaiter().GetResult(); ;

            //TempAsync().GetAwaiter().GetResult();
        }

        private async Task TempAsync()
        {
            // A dummy awaited function call.
            await Task.Delay(0);
            // Add any temporary code.

            Console.WriteLine("Hi");
            
            
            Console.WriteLine("H2");

        }
        // Example code (never called)
        private async Task TestDirectYouTubeChannel()
        {
            JObject json = new JObject();
            json.Add("isChannel", "1");
            var x =await _rpcClient.PythonServerClient.GetYoutubePlaylistRPCAsync(new CTGrpc.PlaylistRequest
            {
                Url = "UCi8e0iOVk1fEOogdfu4YgfA",
                Metadata = new CTGrpc.JsonString() { Json = json.ToString() },


            });
            JArray jArray = JArray.Parse(x.Json);

            Console.WriteLine(x.Json);
        }
    }
}
