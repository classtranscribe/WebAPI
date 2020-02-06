using ClassTranscribeDatabase;
using System.Threading.Tasks;

namespace TaskEngine.Tasks
{
    class CreateBoxTokenTask : RabbitMQTask<string>
    {
        private Box _box;
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.CreateBoxToken, "_1");
        }
        public CreateBoxTokenTask(RabbitMQConnection rabbitMQ, Box box)
        {
            Init(rabbitMQ);
            _box = box;
        }

        protected async override Task OnConsume(string authCode = "")
        {
            await _box.CreateAccessTokenAsync(authCode);
        }
    }
}
