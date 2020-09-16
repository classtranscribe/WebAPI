using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.CryptoPro;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

/// <summary>
/// Implements message queues using RabbitMQ.
/// </summary>
/// <remarks>
/// See https://www.rabbitmq.com/dotnet-api-guide.html
/// Todo/Toreview: https://www.rabbitmq.com/dotnet-api-guide.html#concurrency-channel-sharing
/// 
/// </remarks>
/// 
namespace CTCommons
{
    public class RabbitMQConnection : IDisposable
    {
        IConnection _connection;
        IModel _channel { get; set; }
       
        private readonly ILogger _logger;
        public RabbitMQConnection(ILogger<RabbitMQConnection> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory()
            {
                HostName = Globals.appSettings.RabbitMQServer,
                UserName = Globals.appSettings.ADMIN_USER_ID,
                Password = Globals.appSettings.ADMIN_PASSWORD
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

        }
        /// <summary>
        /// Publishes a Task to the message queue (creating the queue if necessary)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="data"></param>
        /// <param name="taskParameters"></param>
        public void PublishTask<T>(string queueName, T data, TaskParameters taskParameters)
        {
            // Caution. The queue is also declared inside ConsumeTask below
            // I assume this was necessary because as soon as some queues start consuming events
            // they might publish a task to different queue?
            // Better? Should we create the queue (in Program.cs) separately from servicing or publishing the queue
            // Or is always (re-)declaring the queue best practices for RabbitNQ?
            lock ((_channel))
            {
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            var taskObject = new TaskObject<T> { Data = data, TaskParameters = taskParameters };
            var body = CommonUtils.MessageToBytes(taskObject);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            // See https://www.rabbitmq.com/dotnet-api-guide.html#concurrency-channel-sharing
            // Use a lock to ensure thread safety
            lock (_channel)
            {
                _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
            }
            
        }
        /// <summary>
        /// Registers task and starts consuming messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="OnConsume"></param>
        public void ConsumeTask<T>(string queueName, Func<T, TaskParameters, Task> OnConsume, ushort concurrency)
        {
            // Caution. The queue is also declard inside PublishTask above
            _logger.LogInformation("Prefetch/ concurrency count " + concurrency);
            lock (_channel)
            {
                _channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                // See https://www.rabbitmq.com/consumer-prefetch.html
                // See https://stackoverflow.com/questions/59493540/what-is-prefetchsize-in-rabbitmq

                _channel.BasicQos(prefetchSize: 0, prefetchCount: concurrency, global: false);
            }
            
            _logger.LogInformation(" [*] Waiting for messages, queueName - {0}", queueName);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var taskObject = CommonUtils.BytesToMessage<TaskObject<T>>(ea.Body);
                _logger.LogInformation(" [x] {0} Received {1}", queueName, taskObject.ToString());
                // TODO: Update JobStatus table here (started timestamp)
                try
                {
                    await OnConsume(taskObject.Data, taskObject.TaskParameters);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occured in RabbitMQConnection {0} for message {1}", queueName, taskObject.ToString());
                }

                _logger.LogInformation(" [x] {0} Done {1}", queueName, taskObject.ToString());
                // TODO Update JobStatus table here (including timestamp +  result + exception if it occurred)
                lock (_channel)
                {
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
            lock (_channel)
            {
                _channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);
            }
        }
        /// <summary>
        /// Deletes all Rabbit MQ queues (currently used in TaskEngine Program.cs during startup)
        /// </summary>
        public void DeleteAllQueues()
        {
            lock (_channel)
            {
                foreach (CommonUtils.TaskType taskType in Enum.GetValues(typeof(CommonUtils.TaskType)))
                {
                    string queueName = taskType.ToString();
                    try
                    {
                        _channel.QueueDelete(queueName);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error deleting queue {0}", queueName);
                    }
                }
            }
            // TODO Update JobStatus table here
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_channel != null && !disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                lock (_channel)
                {
                    _channel.Close(); _channel = null;
                    _connection.Close(); _connection = null;
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~RabbitMQConnection()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
    public class TaskParameters
    {
        //TODO /TOREVIEW: This is checked in a few places, but is it set anywhere?
        public bool Force { get; set; }
        public JObject Metadata { get; set; }

        /// <summary>
        /// Returns a  readable String for debugging and logging purposes.
        /// </summary>
        /// <returns>A possibly verbose String representation of this object.</returns>
        public override string ToString()
        {
            return $"TaskParameters(Force = {Force}; Metadata = {Metadata})";
        }
    }

    public class TaskObject<T>
    {
        public T Data { get; set; }
        public TaskParameters TaskParameters { get; set; }
        /// <summary>
        /// Returns a  readable String for debugging and logging purposes.
        /// </summary>
        /// <returns>A possibly verbose String representation of this object.</returns>
        public override String ToString()
        {
            return $"TaskObject(Data={Data}; TaskParameters={TaskParameters};";
        }
    }
}
