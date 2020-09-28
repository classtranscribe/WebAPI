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
            // Maybe in the future if we use this task again: registerTask(cleanup, "RefreshAccessTokenAsync"); // may throw AlreadyInProgress exception
            // no. xx nope await _box.RefreshAccessTokenAsync();
            // refreshing the Box access token caused the token to go stale
            // We've had a better experience not refreshing it
        }        
    }
}
