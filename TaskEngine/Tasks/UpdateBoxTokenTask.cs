using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class UpdateBoxTokenTask : RabbitMQTask<string>
    {
        private Box _box;
        public UpdateBoxTokenTask(RabbitMQConnection rabbitMQ, Box box, ILogger<UpdateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.UpdateBoxToken, logger)
        {
            _box = box;
        }

        protected async override Task OnConsume(string emptyString = "")
        {
            await _box.RefreshAccessTokenAsync();
        }
    }
}
