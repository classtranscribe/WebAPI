using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class CreateBoxTokenTask : RabbitMQTask<string>
    {
        private readonly BoxAPI _box;
        public CreateBoxTokenTask(RabbitMQConnection rabbitMQ, BoxAPI box, ILogger<CreateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.CreateBoxToken, logger)
        {
            _box = box;
        }

        protected async override Task OnConsume(string authCode, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, "CreateAccessTokenAsync");
            await _box.CreateAccessTokenAsync(authCode);
        }
    }
}
