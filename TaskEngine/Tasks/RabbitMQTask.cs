using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine
{
    public abstract class RabbitMQTask<T>
    {
        protected RabbitMQConnection _rabbitMQ { get; set; }
        protected string _queueName;
        protected readonly ILogger _logger;

        public RabbitMQTask() { }

        public RabbitMQTask(RabbitMQConnection rabbitMQ, TaskType taskType, ILogger logger)
        {
            _rabbitMQ = rabbitMQ;
            _queueName = taskType.ToString();
            _logger = logger;
        }

        public void Publish(T data, TaskParameters taskParameters = null)
        {
            try
            {
                if(taskParameters == null)
                {
                    taskParameters = new TaskParameters();
                }
                _rabbitMQ.PublishTask(_queueName, data, taskParameters);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Publishing Task!");
            }
        }

        protected abstract Task OnConsume(T data, TaskParameters taskParameters = null);
        public void Consume()
        {
            try
            {
                _rabbitMQ.ConsumeTask<T>(_queueName, OnConsume);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQTask Error Occured");
            }
        }
    }
}
