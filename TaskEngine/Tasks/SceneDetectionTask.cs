using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class SceneDetectionTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly LocalTranscriptionTask _transcriptionTask;

        public SceneDetectionTask(RabbitMQConnection rabbitMQ,LocalTranscriptionTask localTanscriptionTask, RpcClient rpcClient, ILogger<SceneDetectionTask> logger)
            : base(rabbitMQ, TaskType.SceneDetection, logger)
        {
            _rpcClient = rpcClient;
            _transcriptionTask = localTanscriptionTask;
        }
        /// <summary>Extracts scene information for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId); // may throw AlreadyInProgress exception
            GetLogger().LogInformation($"SceneDetection({videoId}): Consuming Task");
            var filepath = "";

            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.FindAsync(videoId);
                
                if (!video.SceneData.HasValues || taskParameters.Force)
                {
                    filepath =  video.Video1.VMPath;
                }
            }
            if(filepath.Length == 0 || ! File.Exists(filepath)) {
                GetLogger().LogInformation($"SceneDetection({videoId}): has no file to process (filepath={filepath})");
                return;
            }
            GetLogger().LogInformation($"SceneDetection({videoId}): GetScenesRPCAsync filepath={filepath})");
            var jsonString = await _rpcClient.PythonServerClient.GetScenesRPCAsync(new CTGrpc.File
            {
                FilePath = filepath
            });
            // 1 hour later... refind the video object
            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.FindAsync(videoId);
            
                JArray scenes = JArray.Parse(jsonString.Json);
                    GetLogger().LogInformation($"SceneDetection({videoId}): Scene count = {scenes.Count}.");
                    video.SceneData = new JObject
                    {
                        { "Scenes", scenes }
                    };
                    
                    var allRawPhrases = new List<string>();
                    foreach (JObject scene in scenes) {
                         allRawPhrases.Add( scene.GetValue("phrases").ToString());
                    }

                    var rawData = string.Join("\n", allRawPhrases );
                    
                    GetLogger().LogInformation($"SceneDetection({videoId}): Raw Phrase Entry Count = {allRawPhrases.Count}. Total String Length = {rawData.Length}");  

                    var phraseResponse = await _rpcClient.PythonServerClient.ToPhraseHintsRPCAsync( new CTGrpc.PhraseHintRequest {
                        RawPhraseData = rawData
                    });
                    var phraseHints = (string)phraseResponse.Result;

                    GetLogger().LogInformation($"SceneDetection({videoId}): phraseHints={phraseHints.Length} characters, {phraseHints.Split("\n").Length} phrases");
                    video.PhraseHints = phraseHints;

                    await _context.SaveChangesAsync();
            }
                GetLogger().LogInformation($"SceneDetection({videoId}): Changes saved. Publishing transcription task request...");
                _transcriptionTask.Publish(videoId);
            
        }
    }
}
