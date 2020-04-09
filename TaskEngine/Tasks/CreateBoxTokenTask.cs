using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class CreateBoxTokenTask : RabbitMQTask<string>
    {
        private readonly BoxAPI _box;
        public CreateBoxTokenTask(RabbitMQConnection rabbitMQ, BoxAPI box, ILogger<CreateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.CreateBoxToken, logger)
        {
            _box = box;
        }

        protected async override Task OnConsume(string authCode, TaskParameters taskParameters)
        {
            await _box.CreateAccessTokenAsync(authCode);
        }
    }
}
