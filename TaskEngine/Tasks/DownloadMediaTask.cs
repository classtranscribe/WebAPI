using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Collections.Generic;
using System.Text;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class DownloadMediaTask : IRabbitMQTask<Media>
    {
        private RabbitMQ _rabbitMQ;
        private CTDbContext _context;
        private string queueName;
        private RpcClient _rpcClient;
        
        private void Init(RabbitMQ rabbitMQ, CTDbContext context)
        {
            _rabbitMQ = rabbitMQ;
            _context = context;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.DownloadMedia, "_1");
        }
        public DownloadMediaTask(RabbitMQ rabbitMQ, CTDbContext context, RpcClient rpcClient)
        {
            Init(rabbitMQ, context);
            _rpcClient = rpcClient;
        }
        public void Consume()
        {
            throw new NotImplementedException();
        }

        public void Publish(Media obj)
        {
            Console.WriteLine(obj);
        }
    }
}
