using Newtonsoft.Json.Linq;
using System;
using static ClassTranscribeDatabase.CommonUtils;

namespace ClassTranscribeDatabase.Models
{

    /// <summary>
    /// A representation of a background job to aid debugging information and provide status information to instructors
    /// </summary>
    /// <remarks>
    /// We dont add a collection of TaskItems to Videos, Playlists etc because the entity framework will then always load these rows
    /// See https://stackoverflow.com/questions/32273760/can-you-exclude-reverse-navigation-properties-in-ef-when-eager-loading
    /// 
    /// </remarks>

    public class TaskItem : Entity
    {
        // e.g. RefreshPlaylist/playlist-id
        // e.g. GenerateCaptions/playlist-uuid
        // e.g. GenerateCaptions/videoid/old-caption-id
        // RuleURIs are not unique; a job may be repeated (e.g. poll external playlists) or repeated due to server restart/service timeout
        // Generally in the form of a action and object (Entity name + uuid) the action will be applied to 
        // They are useful for searching for similar jobs
        // MUST be set
        // Future: Preventing retries and silencing alarms will be implemented by matching a particular rule string e.g. don't download video 123
        public string Rule { get; set; }

        // TODO/TOREVIEW: Ths field already existed, and has an alternate key defined. Consider removing it in the future  
        public string UniqueId { get; set; }

        // Transcribe DownloadVideo etc
        public TaskType TaskType { get; set; }

        //public int Attempts { get; set; }
        public JObject TaskParameters { get; set; }
        //public bool Result { get; set; }
        //public bool Retry { get; set; }


        // -- Timing information

        public DateTime? QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public DateTime? EstimatedCompletionAt { get; set; }
        // At some point, tasks may publish partial completion data
        public int PercentComplete { get; set; }


        // -- Navigation to related tasks, to be able to explore the task tree


        // If this Task is a repeat of previously failed task.
       
        public string PreviousAttemptTaskItemId { get; set; }
        public int AttemptNumber { get; set; }
      
        
        public String ParentTaskItemId { get; set; }

        /// <summary>
        /// The utlimate reason for all a tree of tasks e.g. PeriodicTask, RefreshTask. The Ancestor has no Parent set and no Ancestor
        /// </summary>
        public string AncestorTaskItemId { get; set; }

        // Context in which this task was created (useful for filtering and searching)
        //https://www.learnentityframeworkcore.com/conventions/one-to-many-relationship#optional-relationships
        // TODO/TOREVIEW: Maybe it is safe to declare these as ForeignKeys in CTDBContext?
        // (I don't know enough about how these declarations will modify the database)

        public String OfferingId { get; set; }
        public String MediaId { get; set; }
        public String PlaylistId { get; set; }
        public String UserId { get; set; }
        public String VideoId { get; set; }


        // Status of Task (from creation to completion
        // See TaskStatusCode enum
        public TaskStatusCode TaskStatusCode { get; set; }

        // -- Debugging Specific Tasks

        // Tasks typically have some C# code and a RPC portion of computation,

        // Printable string intended for display for task developers to help development and debugging of a task in production
        // There are no constraints on how, if or when this is updated by specific tasks
        public String DebugMessage { get; set; }

        // Results (including exception details) of local C# processing
        public JObject ResultData { get; set; } // Json {"Exception":{ "Type":"","Message":"","StackTrack":""}, result:""}
                                                // Results from remote processing (including exception details)

        // Many Tasks use some remote procedure call first. Log the details of that phase
        public JObject RemoteResultData { get; set; } // Json {"Exception":{ "Type":"","Message":"","StackTrack":""}, result:""}

        // -- RabbitMQ Specific information

        // Rabbit MQ message identifier if known
        public String OpaqueMessageRef { get; set; }

    }


    public enum TaskStatusCode
    {
        // Future codes will always respect primaryreason = value - (value % 100)
        // e.g. 300s will always be Removed 

        Created = 100, // Entry in this table but not in the queue

        Queued = 200, // Entry in the queue

        Removed = 300, // Entry has been removed from queue, was never started. No reason given
        RemoveddByRestart = 310, // Removed due to external container or VM restart
        RemoveByTaskEngine = 320, // TaskEngine removed this task due to some logical reason 
        RemovedBySelf = 330, // The Task self-canceled ; this is impossible but kept to synchronized values with the cancelled ones
        RemovedByAdmin = 340, // Extenrnally canceled by an admin web user or swag api/admin interface
        RemovedByWebUser = 350, // Extenrnally canceled by a (non admin) web user

        // Codes below 400-599 represent started

        Running = 400, // Computation has started, no further information
        RunningStartingUp = 410, // Collecting data 
        RunningProcessing = 420, // Running main processing
        RunningRemoteProcessing = 430, // Computation is running remotedly
        RunningFinalizing = 440, // Final database saves and updates in progress 

        // Example future extension: Paused=405, // Computation has paused (not currently implemented)
        // 500s and 600s currently used

        // Codes 700 or above represent completion of some kind after running
        Succeeded = 700, // Finished normaly
        SucceededWithErrors = 710, // Task finished normally but data errors should be reviewed 

        // Cancelled jobs may typically be retried if the cause is understood
        Cancelled = 800, // Canceled, no further reason
        CancelledByRestart = 810, // Extenrnally canceled due to external container or VM restart
        CancelledByTaskEngine = 820, // TaskEngine canceled this task 
        CancelledBySelf = 830, // The Task self-canceled due to unmet pre-condition
        CancelledByAdmin = 840, // Extenrnally canceled by an admin web user or swag api/admin interace
        CancelledByWebUser = 850, // Extenrnally canceled by a (non admin) web user

        // Failed jobs should generally not be restarted
        Failed = 900, // Job ran to completion but an error occurred
        FailedDueToRPCProtocol = 910, // Job failed due to gRPC communcationerror (protocol error, not remote code/data error)
        FailedDueToRemoteError = 920, // Job failed during remote computation
        FailedDueToTimeout = 930, // A timeout condition occured and this computation was discarded
        FailedDueToMessageBus = 940
    }
}


