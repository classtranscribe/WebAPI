using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;


// #pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DescribeVideoTask : RabbitMQTask<string>
    {
        private readonly DescribeImageTask _describeImageTask;
 
        public DescribeVideoTask(RabbitMQConnection rabbitMQ, DescribeImageTask describeImageTask, ILogger<DescribeVideoTask> logger)
            : base(rabbitMQ, TaskType.DescribeVideo, logger)
        {
            _describeImageTask = describeImageTask;
        }
        /// <summary>Extracts scene descriptions for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId); // may throw AlreadyInProgress exception
            GetLogger().LogInformation($"DescribeVideoTask({videoId}): Consuming Task");

            using var _context = CTDbContext.CreateDbContext();
            Video video = await _context.Videos.FindAsync(videoId);

            if (!video.HasSceneObjectData())
            {
                GetLogger().LogInformation($"Describe Video {videoId}: Early return - no scene data to process");
                return;
            }
            TextData td = await _context.TextData.FindAsync(video.SceneObjectDataId);

            JObject sceneData = td.GetAsJSON() as JObject;
            JArray scenes = sceneData["Scenes"] as JArray;
            var captions = new List<Caption>();

            const string SIR = "ClassTranscribe/Scene-Describe"; // todo move into Model e.g. CaptionConstants
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Transcription? transcription = video.Transcriptions.Where(t => t.SourceInternalRef == SIR).FirstOrDefault();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            if (transcription == null)
            {
                var theLanguage = Languages.ENGLISH_AMERICAN;

                transcription = new Transcription()
                {
                    TranscriptionType = TranscriptionType.TextDescription,
                    VideoId = video.Id,
                    Language = Languages.ENGLISH_AMERICAN,
                    Label = "Description",
                    SourceLabel = "ClassTranscribe",
                    SourceInternalRef = SIR
                };
                GetLogger().LogInformation($"Describe Video {videoId}: Creating new (empty) Description Entry");

                _context.Add(transcription);
                await _context.SaveChangesAsync();
            }
            else
            {
                captions = transcription.Captions.ToList();
                GetLogger().LogInformation($"{videoId}: Reusing Description. Found {captions.Count} captions");
            }
            // Step 2 Create Placeholder captions if they don't exist for every scene.
            int alreadyDoneCount = 0, taskEnqueueCount = 0; ;

            int CaptionIndex = captions != null && captions.Count > 0 ? captions.Select(c => c.Index).Max() + 1 : 0;
            var newCaptions = new List<Caption>();
            var scenesWithNewCaption = new List<int>();
            var describeScenes = new List<int>();
            int sceneIndex = 0;
            foreach (JObject scene in scenes)
            {
                var captionId = scene["captionId"]?.ToString();
                var caption = captionId != null ? captions.Where(c => c.Id == captionId).FirstOrDefault() : null;
                if (caption == null)
                {
                    var c = new Caption
                    {
                        Index = CaptionIndex++,
                        Begin = TimeSpan.Parse(scene["start"].ToString()),
                        End = TimeSpan.Parse(scene["end"].ToString()),
                        CaptionType = CaptionType.AudioDescription,
                        Text = CaptionConstants.PlaceHolderText,
                        TranscriptionId = transcription.Id
                    };
                    newCaptions.Add(c);
                    scenesWithNewCaption.Add(sceneIndex);
                    describeScenes.Add(sceneIndex);
                }
                else
                {
                    if (caption.HasPlaceHolderText())
                    {
                        describeScenes.Add(sceneIndex);
                    }
                    else
                    {
                        alreadyDoneCount++;
                    }
                }

                sceneIndex++; // todo rewrite as map with index?                
            }

            GetLogger().LogInformation($"Describe Video {videoId}: ${newCaptions.Count} new captions to create");
            if (newCaptions.Any())
            {
                _context.AddRange(newCaptions);
                await _context.SaveChangesAsync();
                // Now we can associate the captionIds with the scene data
                foreach (int i in scenesWithNewCaption)
                {
                    dynamic scene = scenes[i] as JObject;
                    scene.captionId = newCaptions[i].Id;
                }
                sceneData.Remove("Scenes");
                sceneData.Add("Scenes", scenes);
                td.SetFromJSON(sceneData);
                _context.Update(td);
                await _context.SaveChangesAsync();
                GetLogger().LogInformation($"Describe Video {videoId}: Scene Data {td.Id} Updated with Caption references");
            }
            GetLogger().LogInformation($"Describe Video {videoId}: {describeScenes.Count} Description Tasks to enqueue");
            foreach (int i in describeScenes)
            {
                JObject scene = scenes[i] as JObject;
                dynamic taskMeta = new JObject();
                string imageFile = scene["img_file"].ToString();
                taskMeta.ImageFile = imageFile;
                taskMeta.OCRText = scene["raw_text"]?.ToString();
                taskMeta.CaptionId = scene["captionId"].ToString();

                taskEnqueueCount++;
                var taskParams = new TaskParameters(taskMeta);

                GetLogger().LogInformation($"Describe Video {videoId}: {imageFile} {taskMeta.CaptionId} {transcription.Id}");
                _describeImageTask.Publish(imageFile, taskParams);

            }
            GetLogger().LogInformation($"Describe Video {videoId}: AlreadyDone={alreadyDoneCount}.enqueueCount={taskEnqueueCount}");
            GetLogger().LogInformation($"Describe Video {videoId}: Returning.");
        }
    }
}