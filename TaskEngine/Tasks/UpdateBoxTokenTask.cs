using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class UpdateBoxTokenTask : RabbitMQTask<string>
    {
        private BoxAPI _box;
        public UpdateBoxTokenTask(RabbitMQConnection rabbitMQ, BoxAPI box, ILogger<UpdateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.UpdateBoxToken, logger)
        {
            _box = box;
        }

        protected async override Task OnConsume(string emptyString, TaskParameters taskParameters)
        {
            //await _box.RefreshAccessTokenAsync();
        }
    }
}
