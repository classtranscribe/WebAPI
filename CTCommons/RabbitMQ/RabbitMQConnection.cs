using ClassTranscribeDatabase;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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
    // Used to keep track of all active running tasks
    // And current tasks per message being consumed
    // Some of the apparent twisted design surrounding the use of this class
    // Is to ensure that we can collect keys in the concrete classes
    // Then remove them in a finally{} block even if the client throws an exception
    public class ClientActiveTasks : HashSet<object>
    {
        public ClientActiveTasks() { }
        public ClientActiveTasks(ClientActiveTasks source) : base(source)
        {
        }

    };

    public class RabbitMQConnection : IDisposable
    {
        // Created by the first instance, then re-used
        private static IConnection _connection;

        IModel _channel { get; set; }
        String _expiration; // milliseconds

        private readonly ILogger _logger;
        public RabbitMQConnection(ILogger<RabbitMQConnection> logger)
        {
            _logger = logger;
            CreateSharedConnection();

            // TODO/TOREVIEW: Check number of threads created
            // Potentially Model can be shared too 
            _channel = _connection.CreateModel();

            uint time = Math.Min(1, Convert.ToUInt32(Globals.appSettings.RABBITMQ_TASK_TTL_MINUTES));
            SetMessageExpiration(time);
        }

        private void CreateSharedConnection()
        {
            if (_connection != null)
            {
                return;
            }
            _logger.LogInformation("Creating RabbitMQ connection");
            var factory = new ConnectionFactory()
            {

                HostName = Globals.appSettings.RABBITMQ_SERVER_NAME.Length > 0 ? Globals.appSettings.RABBITMQ_SERVER_NAME : Globals.appSettings.RabbitMQServer,
                UserName = Globals.appSettings.ADMIN_USER_ID,
                Password = Globals.appSettings.ADMIN_PASSWORD,
                Port = Convert.ToUInt16(Globals.appSettings.RABBITMQ_PORT) // 5672

            };
            // A developer may still want to checkout old code which uses the old env branch
            // so just complain loudly for now
            // In 2021 we can remove support for the old variable
            if (Globals.appSettings.RabbitMQServer.Length > 0)
            {
                _logger.LogError("*** Mixed case 'RabbitMQServer' environment variable is deprecated. Review your .env or vs_appsettings.json environment settings");
                if (Globals.appSettings.RABBITMQ_SERVER_NAME.Length == 0)
                {
                    _logger.LogError("*** Update your environment to use RABBITMQ_SERVER_NAME.");
                }
                else if (Globals.appSettings.RABBITMQ_SERVER_NAME != Globals.appSettings.RabbitMQServer)
                {
                    {
                        _logger.LogError("*** RABBITMQ_SERVER_NAME and RabbitMQServer are both set and different! Using RABBITMQ_SERVER_NAME");
                    }
                }
            }

            _logger.LogInformation($"Connecting to RabbitMQ server {factory.HostName} with user {factory.UserName} on port {factory.Port}...");
            _connection = factory.CreateConnection();
        }


        public void SetMessageExpiration(uint ttlMinutes)
        {

            uint OneMinuteAsMilliseconds = 1000 * 60;
            _expiration = (OneMinuteAsMilliseconds * ttlMinutes).ToString();
            _logger.LogInformation("Using Message TTL {0} minutes", ttlMinutes);
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

            // Note delivered but unacked messages do not expire
            properties.Expiration = _expiration; // milliseconds

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
        public void ConsumeTask<T>(string queueName, Func<T, TaskParameters, ClientActiveTasks, Task> OnConsume, Func<ClientActiveTasks, int> PostConsumeCleanup, ushort concurrency)
        {
            // Caution. The queue is also declard inside PublishTask above
            _logger.LogInformation("Prefetch concurrency count {0}", concurrency);
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
                    // This object exists so that we can wrap all OnConsumes with a try-finally here
                    // And during the finally block remove the task from the set of active tasks
                    // At this level of the code we don't have the specific task information
                    // Instead the specific task my register a task by calling register
                    ClientActiveTasks clientCleanup = new ClientActiveTasks();

                var taskObject = CommonUtils.BytesToMessage<TaskObject<T>>(ea.Body);
                _logger.LogInformation(" [x] {0} Received {1}", queueName, taskObject.ToString());
                    // TODO: Update JobStatus table here (started timestamp)
                    try
                {
                    await OnConsume(taskObject.Data, taskObject.TaskParameters, clientCleanup);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occured in RabbitMQConnection {0} for message {1}", queueName, taskObject.ToString());
                }
                finally
                {
                    PostConsumeCleanup(clientCleanup);

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

        public void PurgeQueue(String queueName)
        {
            lock (_channel)
            {
                try
                {
                    var count = _channel.MessageCount(queueName);
                    _logger.LogInformation("Purging queue {0}: {1} message(s) will be removed", queueName, count);

                    _channel.QueuePurge(queueName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error purging queue {0}", queueName);
                    throw e;
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
