using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace ClassTranscribeDatabase.Models
{
    /// <summary>
    /// The models of ClassTranscribe are defined using Code-First approach.
    /// For more info, https://www.entityframeworktutorial.net/code-first/what-is-code-first.aspx
    /// 
    /// To make any changes to the database schema, the corresponding classes would have to be changed and 
    /// migrations, for more info.
    /// 
    /// https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli
    /// https://www.learnentityframeworkcore.com/migrations
    /// 
    /// Note: To apply migration using the Package Manager Console, ensure that the selected "Default Project" is 
    /// "ClassTranscribeDatabase"
    /// </summary>
    public enum AccessTypes
    {
        Public,
        AuthenticatedOnly,
        StudentsOnly,
        UniversityOnly,
    }
    public enum Status
    {
        Active,
        Deleted
    }

    public enum SourceType
    {
        Echo360,
        Youtube,
        Local,
        Kaltura,
        Box
    }

    public enum Visibility 
    {
        Visible,
        Hidden
    }

    /// <summary>
    /// This class represents a User of ClassTranscribe.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [IgnoreDataMember]
        public virtual List<UserOffering> UserOfferings { get; set; }
        [IgnoreDataMember]
        public string UniversityId { get; set; }
        public virtual University University { get; set; }
        public Status Status { get; set; }
        public JObject Metadata { get; set; }
    }

    /// <summary>
    /// This is a base class to define common properties for all Tables.
    /// 
    /// </summary>
    public abstract class Entity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        [IgnoreDataMember]
        public DateTime CreatedAt { get; set; }
        [IgnoreDataMember]
        public string CreatedBy { get; set; }
        [IgnoreDataMember]
        public DateTime LastUpdatedAt { get; set; }
        [IgnoreDataMember]
        public string LastUpdatedBy { get; set; }
        [IgnoreDataMember]
        public Status IsDeletedStatus { get; set; }

        public ResourceType GetResourceType()
        {
            switch (this)
            {
                case Course _:
                    return ResourceType.Course;
                case Offering _: return ResourceType.Offering;
                case Playlist _: return ResourceType.Playlist;
                case Media _: return ResourceType.Media;
            }
            throw new InvalidOperationException("Invalid Type passed" + this);
        }
    }

    public class University : Entity
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        [IgnoreDataMember]
        public virtual List<Department> Departments { get; set; }
        [IgnoreDataMember]
        public virtual List<Term> Terms { get; set; }
    }

    public class Department : Entity
    {
        public string Name { get; set; }
        public string Acronym { get; set; }
        [IgnoreDataMember]
        public virtual List<Course> Courses { get; set; }
        public string UniversityId { get; set; }
        [IgnoreDataMember]
        public virtual University University { get; set; }
    }

    public class Course : Entity
    {
        public string CourseNumber { get; set; }
        public string DepartmentId { get; set; }
        [IgnoreDataMember]
        public virtual Department Department { get; set; }
        [IgnoreDataMember]
        public virtual List<CourseOffering> CourseOfferings { get; set; }
    }

    public class Term : Entity
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UniversityId { get; set; }
        [IgnoreDataMember]
        public virtual University University { get; set; }
        [IgnoreDataMember]
        public virtual List<Offering> Offerings { get; set; }
    }
    public class Offering : Entity
    {
        public string SectionName { get; set; }
        public string TermId { get; set; }
        [IgnoreDataMember]
        public virtual Term Term { get; set; }
        [IgnoreDataMember]
        public virtual List<CourseOffering> CourseOfferings { get; set; }
        [IgnoreDataMember]
        public virtual List<Playlist> Playlists { get; set; }
        [IgnoreDataMember]
        public virtual List<UserOffering> OfferingUsers { get; set; }
        public AccessTypes AccessType { get; set; }
        public bool LogEventsFlag { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public JObject JsonMetadata { get; set; }
        public Visibility Visibility { get; set; }
    }

    public class Playlist : Entity
    {
        public string Name { get; set; }
        public SourceType SourceType { get; set; }
        public string PlaylistIdentifier { get; set; }
        public virtual List<Media> Medias { get; set; }
        public string OfferingId { get; set; }
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
        public JObject JsonMetadata { get; set; }
        public int Index { get; set; }
        public Visibility Visibility { get; set; }
    }

    public class Media : Entity
    {
        public SourceType SourceType { get; set; }
        public string UniqueMediaIdentifier { get; set; }
        public JObject JsonMetadata { get; set; }
        public string VideoId { get; set; }
        [IgnoreDataMember]
        public virtual Video Video { get; set; }
        public string PlaylistId { get; set; }
        [IgnoreDataMember]
        public virtual Playlist Playlist { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public virtual List<WatchHistory> WatchHistories { get; set; }
        public Visibility Visibility { get; set; }
    }

    public class Transcription : Entity
    {
        [ForeignKey("File")]
        public string FileId { get; set; }
        public virtual FileRecord File { get; set; }
        public virtual FileRecord SrtFile { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string VideoId { get; set; }
        [IgnoreDataMember]
        public virtual Video Video { get; set; }

        [IgnoreDataMember]
        public virtual List<Caption> Captions { get; set; }
    }

    public class Video : Entity
    {
        [ForeignKey("Video1")]
        public string Video1Id { get; set; }

        public virtual FileRecord Video1 { get; set; }

        [ForeignKey("Video2")]
        public string Video2Id { get; set; }
        public virtual FileRecord Video2 { get; set; }
        [ForeignKey("ProcessedVideo1")]
        public string ProcessedVideo1Id { get; set; }

        public virtual FileRecord ProcessedVideo1 { get; set; }

        [ForeignKey("ProcessedVideo2")]
        public string ProcessedVideo2Id { get; set; }
        public virtual FileRecord ProcessedVideo2 { get; set; }

        [ForeignKey("Audio")]
        public string AudioId { get; set; }
        public virtual FileRecord Audio { get; set; }

        public string Description { get; set; }
        public virtual List<Media> Medias { get; set; }
        public string TranscriptionStatus { get; set; }
        public int TranscribingAttempts { get; set; }
        public virtual List<Transcription> Transcriptions { get; set; }

        public virtual List<EPub> EPubs { get; set; }
        public JObject SceneData { get; set; }
        public JObject JsonMetadata { get; set; }

        public async Task DeleteVideoAsync(CTDbContext context)
        {
            if (Video1 != null)
            {
                await Video1.DeleteFileRecordAsync(context);
            }
            if (Video2 != null)
            {
                await Video2.DeleteFileRecordAsync(context);
            }
            if (ProcessedVideo1 != null)
            {
                await ProcessedVideo1.DeleteFileRecordAsync(context);
            }
            if (ProcessedVideo2 != null)
            {
                await ProcessedVideo2.DeleteFileRecordAsync(context);
            }
            if (Audio != null)
            {
                await Audio.DeleteFileRecordAsync(context);
            }
            var dbVideoRow = await context.Videos.FindAsync(Id);
            if (dbVideoRow != null)
            {
                context.Videos.Remove(dbVideoRow);
                await context.SaveChangesAsync();
            }
        }
    }

    public class CourseOffering : Entity
    {
        public string CourseId { get; set; }
        public string OfferingId { get; set; }
        [IgnoreDataMember]
        public virtual Course Course { get; set; }
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }

    }

    public class UserOffering : Entity
    {
        public string OfferingId { get; set; }
        public string ApplicationUserId { get; set; }
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
        [IgnoreDataMember]
        public virtual ApplicationUser ApplicationUser { get; set; }
        public string IdentityRoleId { get; set; }
        [IgnoreDataMember]
        public virtual IdentityRole IdentityRole { get; set; }
    }

    public class Log : Entity
    {
        public string UserId { get; set; }
        public string OfferingId { get; set; }
        public string MediaId { get; set; }
        public string EventType { get; set; }
        public JObject Json { get; set; }
    }

    public class EPub : Entity
    {
        public string Language { get; set; }
        [ForeignKey("File")]
        public string FileId { get; set; }
        public virtual FileRecord File { get; set; }
        public string VideoId { get; set; }
        [IgnoreDataMember]
        public virtual Video Video { get; set; }
        public JObject Json { get; set; }
        public virtual List<EPubChapter> EPubChapters { get; set; }
    }

    public class EPubChapter : Entity
    {
        public string EPubId { get; set; }
        [IgnoreDataMember]
        public virtual EPub EPub { get; set; }
        public JObject Data { get; set; }
    }

    public class WatchHistory : Entity
    {
        public string MediaId { get; set; }
        [IgnoreDataMember]
        public virtual Media Media { get; set; }
        public string ApplicationUserId { get; set; }
        [IgnoreDataMember]
        public virtual ApplicationUser ApplicationUser { get; set; }
        public JObject Json { get; set; }
    }

    public class Dictionary : Entity
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public enum ResourceType 
    {
        Offering,
        Course,
        Media,
        Playlist
    }

    public enum Ack
    {
        Pending,
        Seen
    }
    
    public class Subscription : Entity
    {
        public ResourceType ResourceType { get; set; }
        public string ResourceId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public string ApplicationUserId { get; set; }
    }

    public class Message : Entity
    {
        public virtual ApplicationUser ApplicationUser { get; set; }
        public string ApplicationUserId { get; set; }
        public JObject Payload { get; set; }
        public LogLevel LogLevel { get; set; }
        public Ack Ack { get; set; }
    }


    /// <summary>

    public class TaskItem : Entity
    {
        // e.g. RefreshPlaylist/playlist-id
        // e.g. GenerateCaptions/playlist-uuid
        // e.g. GenerateCaptions/videoid/old-caption-id
        // RuleURIs are not unique; a job may be repeated (e.g. poll external playlists) or repeated due to server restart/service timeout
        // They are useful for searching for similar jobs
        public string RuleURI {get; set;}
        public string UniqueId { get; set; }

        // Transcribe DownloadVideo etc
        public TaskType TaskType { get; set; }
        //public int Attempts { get; set; }
        public JObject TaskParameters { get; set; }
        //public bool Result { get; set; }
        //public bool Retry { get; set; }
        
        
        public DateTime QueuedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }

        public TaskItem PreviousAttempt { get; set; }
        public int AttemptNumber { get; set; }
        public TaskItem Parent { get; set; }
        // Context in which this task was created (useful for filtering and searching)
        public String OfferingId { get; set; }
        public String MediaId { get; set; }
        public String PlaylistId { get; set; }
        public String UserId { get; set; }
        public String VideoId { get; set; }

        // If another Task was ultimately responsible for this task
        public TaskItem Ancestor { get; set; }

        public TaskResultCode TaskResultCode { get; set; }
        // Printable string that explains the ultimate status of this task. Typically updated when Completion status is set
        public String DisplayMessage { get; set; }

        // Results (including exception details) of local C# processing
        public JObject ResultData { get; set; } // Json {"Exception":{ "Type":"","Message":"","StackTrack":""}, result:""}
        // Results from remote processing (including exception details)
        
        // Many Tasks use some remote procedure call first. Log the details of that phase
        public JObject RemoteResultData { get; set; } // Json {"Exception":{ "Type":"","Message":"","StackTrack":""}, result:""}
        
        // Rabbit MQ message identifier if known
        public String OpaqueMessageRef { get; set; }

    }


    public enum TaskResultCode
    {
        Created = 100, // Entry in this table but not in the queue
        Queued = 200, // Entry in the queue
        Removed = 300, // Entry has been removed from queue, was never started. No reason given
        RemoveddByRestart = 305, // Removed due to external container or VM restart
        RemoveByTaskEngine = 310, // TaskEngine removed this task due to some logical reason 
        RemovedBySelf = 715, // The Task self-canceled ; this is impossible but kept to synchronized values with the cancelled ones
        RemovedByAdmin = 720, // Extenrnally canceled by an admin web user or swag api/admin interface
        RemovedByWebUser = 725, // Extenrnally canceled by a (non admin) web user



        // Codes below 400-599 represent started

        Started = 400, // Computation has started!
                       //xFuture? Paused=500, // Computation has paused (not currently implemented)

        // Codes 600 or above represent completion
        Succeeded = 600, // Finished normaly
        SucceededWithWarning = 605, // Task finished normally but data errors should be reviewed 

        // Cancelled jobs may typically be retried if the cause is understood
        Cancelled= 700, // Canceled, no further reason
        CancelledByRestart = 705, // Extenrnally canceled due to external container or VM restart
        CancelledByTaskEngine = 710, // TaskEngine canceled this task 
        CancelledBySelf = 715, // The Task self-canceled due to unmet pre-condition
        CancelledByAdmin = 720, // Extenrnally canceled by an admin web user or swag api/admin interace
        CancelledByWebUser = 725, // Extenrnally canceled by a (non admin) web user

        // Failed jobs should generally not be restarted
        Failed = 800, // Job ran to completion but an error occurred somewhere
        FailedDueToRPCProtocol = 805, // Job failed due to gRPC communcationerror (protocol error, not remote code/data error)
        FailedDueToRemoteError = 810, // Job failed during remote computation

        FailedDueToTimeout = 815 // A timeout condition occured and this computation was discarded 
    }

}
