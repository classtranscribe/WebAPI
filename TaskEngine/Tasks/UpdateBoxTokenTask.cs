using CTCommons;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;
using System.Diagnostics.CodeAnalysis;


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class UpdateBoxTokenTask : RabbitMQTask<string>
    {
        private BoxAPI _box;
        public UpdateBoxTokenTask(RabbitMQConnection rabbitMQ, BoxAPI box, ILogger<UpdateBoxTokenTask> logger)
            : base(rabbitMQ, TaskType.UpdateBoxToken, logger)
        {
            _box = box;
        }
#pragma warning disable 1998
        protected async override Task OnConsume(string emptyString, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            //registerTask(cleanup, "RefreshAccessTokenAsync"); // may throw AlreadyInProgress exception
            //await _box.RefreshAccessTokenAsync();
        }        
    }
}
