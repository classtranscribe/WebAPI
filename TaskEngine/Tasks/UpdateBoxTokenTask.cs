using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

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
