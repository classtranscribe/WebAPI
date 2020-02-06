using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class EPubGeneratorTask : RabbitMQTask<EPub>
    {
        private RpcClient _rpcClient;

        public EPubGeneratorTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<EPubGeneratorTask> logger)
            : base(rabbitMQ, TaskType.GenerateEPubFile, logger)
        {
            _rpcClient = rpcClient;
        }

        protected async override Task OnConsume(EPub epub)
        {

            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.FindAsync(epub.VideoId);

                if (video.SceneData == null)
                {
                    var jsonString = await _rpcClient.PythonServerClient.GetScenesAsync(new CTGrpc.File
                    {
                        FilePath = video.Video1.VMPath
                    });
                    JArray scenes = JArray.Parse(jsonString.Json);

                    video.SceneData = new JObject();
                    video.SceneData.Add("Scenes", scenes);

                    await _context.SaveChangesAsync();
                }

                var query = new CaptionQueries(_context);
                var captions = await query.GetCaptionsAsync(epub.VideoId, epub.Language);
                var filePath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, epub.VideoId + "_" + epub.Language + ".epub");
                var file = new FileRecord(filePath);

                CTGrpc.EPubData data = new CTGrpc.EPubData
                {
                    File = file.VMPath,
                    Author = "ClassTranscribe",
                    Title = "Demo Title",
                    Publisher = "ClassTranscribe"
                };
                data.Chapters.AddRange(GetEPubChapters(video.SceneData["Scenes"] as JArray, captions));

                var result = await _rpcClient.NodeServerClient.CreateEPubRPCAsync(data);
                epub.File = new FileRecord(filePath);
                await _context.SaveChangesAsync();
            }
        }
        public List<CTGrpc.EPubChapter> GetEPubChapters(JArray scenes, List<Caption> captions)
        {
            var chapters = new List<CTGrpc.EPubChapter>();
            var nextStart = new TimeSpan(0);
            foreach (JObject scene in scenes)
            {
                var endTime = TimeSpan.Parse(scene["end"].ToString());
                var subset = captions.Where(c => c.Begin < endTime && c.Begin >= nextStart).ToList();
                StringBuilder sb = new StringBuilder();
                subset.ForEach(c => sb.Append(c.Text + " "));
                string allText = sb.ToString();
                chapters.Add(new CTGrpc.EPubChapter
                {
                    Image = new CTGrpc.File { FilePath = scene["img_file"].ToString() },
                    Text = allText
                });
                nextStart = endTime;
            }
            return chapters;
        }
    }
}
