using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread


namespace TaskEngine
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    public abstract class RabbitMQTask<T>
    {
        private RabbitMQConnection _rabbitMQ { get; set; }
        private string _queueName;
        private readonly ILogger _logger;

        // All access to _inProgress and _unregisterLater should be locked using _inProgress
        // We keep track of all active tasks for this process
        // Note  during message consumption there is a another ClientActiveTasks object which tracks
        // the task currently running for that message
        private static Dictionary<string, ClientActiveTasks> _inProgress = new Dictionary<string, ClientActiveTasks>();
       

        public RabbitMQTask() { }

        public RabbitMQTask(RabbitMQConnection rabbitMQ, TaskType taskType, ILogger logger)
        {
            _rabbitMQ = rabbitMQ;
            _queueName = taskType.ToString();
            _logger = logger;
            lock (_inProgress)
            {
                if (!_inProgress.ContainsKey(_queueName))
                {
                    _inProgress.Add(_queueName, new ClientActiveTasks());
                }
            }
        }
        public void PurgeQueue()
        {
            _rabbitMQ.PurgeQueue(_queueName);
        }

        public void Publish(T data, TaskParameters taskParameters = null)
        {
            try
            {
                if (taskParameters == null)
                {
                    taskParameters = new TaskParameters();
                }
                _logger.LogInformation($"Publish Task ({_queueName}). data=({data})");
                _rabbitMQ.PublishTask(_queueName, data, taskParameters);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Publishing Task to {_queueName} !");
            }
        }

        protected abstract Task OnConsume(T data, TaskParameters taskParameters, ClientActiveTasks cleanup);
        protected int PostConsumeCleanup(ClientActiveTasks cleanup)
        {
            if (cleanup == null)
            {
                return 0;
            }

            lock (_inProgress)
            {
                foreach(object id in cleanup)
                
                {
                    bool removed = _inProgress[_queueName].Remove(id);
                    if(!removed)
                    {
                        _logger.LogError($"_inProgress Q {_queueName} failed to remove '{id}'");
                    }

                }
            }
            cleanup.Clear();
            return 0; //Func<> must declare a return type; cannot be void
        }

        public void Consume(ushort concurrency)
        {
            if (concurrency == 0) {
                return; // note this means the queue will not be purged 
            }
            try
            {
                _rabbitMQ.ConsumeTask<T>(_queueName, OnConsume, PostConsumeCleanup, concurrency);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQTask Consume()->ConsumeTask Error Occured on Queue {0}", _queueName);
            }
        }

        /// <summary>
        /// Throws InProgressException if this task is already running
        /// </summary>
        /// <param name="keyId"></param>
        public void registerTask(HashSet<object> cleanup, Object keyId)
        {
            if (cleanup == null || keyId == null)
            {
                return;
            }
            bool alreadyRunning;

            lock (_inProgress)
            {
                if (cleanup.Contains(keyId))
                {
                    // This is a programming error the same message may not register the same key twice
                    throw new Exception($"Cleanup set may not already contain key ({keyId.ToString()})");
                }
                // Now check that globally there is no other task working on the same id
                // This may happen rarely. The purpose of registerTask is to immediately stop (by throwing an exception) if we discover we are late to the party.
                alreadyRunning = !_inProgress[_queueName].Add(keyId);
            }
            if (alreadyRunning)
            {
                _logger.LogError("{0} for {1} Task already running, so skipping this request and throwing exception", _queueName, keyId);
                throw new InProgressException($"{ _queueName} for {keyId} Task already running, so skipping this request");
            }

            cleanup.Add(keyId);

        }
        /// <summary>
        /// Returns a new shallow copy of the current task set
        /// </summary>
        /// <returns></returns>
        public ClientActiveTasks GetCurrentTasks()
        {
            lock (_inProgress)
            {
                return new ClientActiveTasks(_inProgress[_queueName]);
            }
        }

        protected ILogger GetLogger()
        {
            return _logger;
        }
    }
    [Serializable]
    public class InProgressException : Exception
    {
        public InProgressException(string message) : base(message) { }

        public InProgressException()
        {
            throw new NotImplementedException();
        }

        public InProgressException(string message, Exception innerException) : base(message, innerException)
        {
            throw new NotImplementedException();
        }

        protected InProgressException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }


}
