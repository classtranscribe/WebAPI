using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class SceneDetectionTask : RabbitMQTask<JobObject<Video>>
    {
        private readonly RpcClient _rpcClient;

        public SceneDetectionTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<SceneDetectionTask> logger)
            : base(rabbitMQ, TaskType.SceneDetection, logger)
        {
            _rpcClient = rpcClient;
        }

        protected async override Task OnConsume(JobObject<Video> j)
        {

            using (var _context = CTDbContext.CreateDbContext())
            {
                Video video = await _context.Videos.FindAsync(j.Data.Id);

                if (video.SceneData == null || j.Force)
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
