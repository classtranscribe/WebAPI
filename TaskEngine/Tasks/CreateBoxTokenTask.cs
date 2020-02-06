using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class CreateBoxTokenTask : RabbitMQTask<string>
    {
        private Box _box;
        public CreateBoxTokenTask(RabbitMQConnection rabbitMQ, Box box, ILogger<CreateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.CreateBoxToken, logger)
        {
            _box = box;
        }

        protected async override Task OnConsume(string authCode = "")
        {
            await _box.CreateAccessTokenAsync(authCode);
        }
    }
}
