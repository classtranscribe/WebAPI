using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using System.Diagnostics.CodeAnalysis;


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class SceneDetectionTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;

        public SceneDetectionTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<SceneDetectionTask> logger)
            : base(rabbitMQ, TaskType.SceneDetection, logger)
        {
            _rpcClient = rpcClient;
        }
        /// <summary>Extracts scene information for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, videoId); // may throw AlreadyInProgress exception
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

                    video.SceneData = new JObject
                    {
                        { "Scenes", scenes }
                    };

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
