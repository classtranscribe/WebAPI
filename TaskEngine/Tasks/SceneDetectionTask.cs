using System.Collections.Generic;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using Grpc.Core;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using System.Diagnostics.CodeAnalysis;


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class SceneDetectionTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly TranscriptionTask _transcriptionTask;

        public SceneDetectionTask(RabbitMQConnection rabbitMQ,TranscriptionTask transcriptionTask, RpcClient rpcClient, ILogger<SceneDetectionTask> logger)
            : base(rabbitMQ, TaskType.SceneDetection, logger)
        {
            _rpcClient = rpcClient;
            _transcriptionTask = transcriptionTask;
        }
        /// <summary>Extracts scene information for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, videoId); // may throw AlreadyInProgress exception
            GetLogger().LogInformation("SceneDetection Consuming" + videoId);
            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.FindAsync(videoId);

                if (video.SceneData == null || taskParameters.Force)
                {
                    var jsonString = await _rpcClient.PythonServerClient.GetScenesRPCAsync(new CTGrpc.File
                    {
                        FilePath = video.Video1.VMPath
                    });
                    JArray scenes = JArray.Parse(jsonString.Json);
                    GetLogger().LogInformation($"{videoId}: Scene count = {scenes.Count}.");
                    video.SceneData = new JObject
                    {
                        { "Scenes", scenes }
                    };
                    
                    var allRawPhrases = new List<string>();
                    foreach (JObject scene in scenes) {
                         allRawPhrases.Add( scene.GetValue("phrases").ToString());
                    }

                    var rawData = string.Join("\n", allRawPhrases );
                    
                    GetLogger().LogInformation($"{videoId}: Raw Phrase Entry Count = {allRawPhrases.Count}. Total String Length = {rawData.Length}");  

                    var phraseResponse = await _rpcClient.PythonServerClient.ToPhraseHintsRPCAsync( new CTGrpc.PhraseHintRequest {
                        RawPhraseData = rawData
                    });
                    var phraseHints = (string)phraseResponse.Result;

                    GetLogger().LogInformation($"{videoId}:phraseHints={phraseHints.Length} characters, {phraseHints.Split("\n").Length} phrases");
                    video.PhraseHints = phraseHints;

                    await _context.SaveChangesAsync();
                    _transcriptionTask.Publish(videoId);
                }
            }
        }
    }
}
