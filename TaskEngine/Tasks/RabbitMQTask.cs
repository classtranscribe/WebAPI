using ClassTranscribeDatabase;
using System;
using System.Threading.Tasks;

namespace TaskEngine
{
    public abstract class RabbitMQTask<T>
    {
        protected RabbitMQConnection _rabbitMQ;
        protected string queueName;
        public void Publish(T obj)
        {
            try
            {
                _rabbitMQ.PublishTask(queueName, obj);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected abstract Task OnConsume(T obj);
        public void Consume()
        {
            try
            {
                _rabbitMQ.ConsumeTask<T>(queueName, OnConsume);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
