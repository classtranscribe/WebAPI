using ClassTranscribeDatabase;
using System;
using System.Threading.Tasks;

namespace TaskEngine
{
    public abstract class RabbitMQTask<T>
    {
        protected RabbitMQ _rabbitMQ;
        protected CTDbContext _context;
        protected string queueName;
        public void Publish(T obj)
        {
            Console.WriteLine(obj);
            _rabbitMQ.PublishTask(queueName, obj);
        }

        protected abstract Task OnConsume(T obj);
        public void Consume()
        {
            _rabbitMQ.ConsumeTask<T>(queueName, OnConsume);
        }
    }

    public enum TaskType
    {
        FetchPlaylistData,
        DownloadMedia,
        ConvertMedia,
        TranscribeMedia
    }
}
