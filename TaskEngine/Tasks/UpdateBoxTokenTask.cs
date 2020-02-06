using ClassTranscribeDatabase;
using System.Threading.Tasks;

namespace TaskEngine.Tasks
{
    class UpdateBoxTokenTask : RabbitMQTask<string>
    {
        private Box _box;
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.UpdateBoxToken, "_1");
        }
        public UpdateBoxTokenTask(RabbitMQConnection rabbitMQ, Box box)
        {
            Init(rabbitMQ);
            _box = box;
        }

        protected async override Task OnConsume(string emptyString = "")
        {
            await _box.RefreshAccessTokenAsync();
        }
    }
}
