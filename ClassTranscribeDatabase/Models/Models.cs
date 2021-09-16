using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Models
{
    /// <summary>
    /// The models of ClassTranscribe are defined using Code-First approach.
    /// For more info, https://www.entityframeworktutorial.net/code-first/what-is-code-first.aspx
    /// 
    /// To make any changes to the database schema, the corresponding classes would have to be changed and 
    /// migrations, for more info.
    /// 
    /// https://www.learnentityframeworkcore.com/migrations (this documentation is more helpful, includes information on how to revert migrations)
    /// https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli
    /// 
    /// Note: To apply migration using the Package Manager Console, ensure that the selected "Default Project" is 
    /// "ClassTranscribeDatabase"
    ///
    /// Steps to create migration using command line:
    /// 1. Make edits to the entity model (in this file or another model file)
    ///     - NOTE: if creating a new entity/table, you must follow the steps in CTDbContext.cs before continuing
    /// 2. Open terminal, navigate to the "ClassTranscribeDatabase" directory
    /// 3. Run "dotnet ef migrations add <name of migration>" to create the migration
    /// 4. To apply the migration to the database, run "dotnet ef database update"
    /// 
    /// </summary>
    public enum AccessTypes
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)
        Public = 0,
        AuthenticatedOnly = 1,
        StudentsOnly = 2,
        UniversityOnly = 3,
    }
    public enum Status
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)
        Active = 0,
        Deleted = 1
    }

    public enum SourceType
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)
        Echo360 = 0,
        Youtube = 1,
        Local = 2,
        Kaltura = 3,
        Box = 4
    }

    public enum Visibility
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)
        Visible = 0,
        Hidden = 1
    }

    public enum PublishStatus
    {
        Published = 0,
        NotPublished = 1
    }

    /// <summary>
    /// This class represents a User of ClassTranscribe.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<UserOffering> UserOfferings { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string UniversityId { get; set; }
        public virtual University University { get; set; }
        public Status Status { get; set; }
        [Required]
        public JObject Metadata { get; set; } = new JObject();
    }

    /// <summary>
    /// This is a base class to define common properties for all Tables.
    /// 
    /// </summary>
    public abstract class Entity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public DateTime CreatedAt { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string CreatedBy { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public DateTime LastUpdatedAt { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string LastUpdatedBy { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public Status IsDeletedStatus { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public DateTime? DeletedAt { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string? DeletedBy { get; set; }

        public ResourceType GetResourceType()
        {
            switch (this)
            {
                case Course _:
                    return ResourceType.Course;
                case Offering _: return ResourceType.Offering;
                case Playlist _: return ResourceType.Playlist;
                case Media _: return ResourceType.Media;
                case EPub _: return ResourceType.EPub;
            }
            throw new InvalidOperationException("Invalid Type passed" + this);
        }
    }

    public class University : Entity
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<Department> Departments { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<Term> Terms { get; set; }
    }

    public class Department : Entity
    {
        public string Name { get; set; }
        public string Acronym { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<Course> Courses { get; set; }
        public string UniversityId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual University University { get; set; }
    }

    public class Course : Entity
    {
        public string CourseNumber { get; set; }
        public string DepartmentId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Department Department { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<CourseOffering> CourseOfferings { get; set; }
    }

    public class Term : Entity
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UniversityId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual University University { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<Offering> Offerings { get; set; }
    }
    public class Offering : Entity
    {
        public string SectionName { get; set; }
        public string TermId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Term Term { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<CourseOffering> CourseOfferings { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<Playlist> Playlists { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual List<UserOffering> OfferingUsers { get; set; }
        public AccessTypes AccessType { get; set; }
        public bool LogEventsFlag { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        [Required]
        public JObject JsonMetadata { get; set; } = new JObject();
        public Visibility Visibility { get; set; }
        public PublishStatus PublishStatus { get; set; }
    }

    public class Playlist : Entity
    {
        public string Name { get; set; }
        public SourceType SourceType { get; set; }
        public string PlaylistIdentifier { get; set; }
        public virtual List<Media> Medias { get; set; }
        public string OfferingId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
        [Required]
        public JObject JsonMetadata { get; set; } = new JObject();
        public int Index { get; set; }
        public Visibility Visibility { get; set; }
        public PublishStatus PublishStatus { get; set; }
    }

    public class Media : Entity
    {
        public SourceType SourceType { get; set; }
        public string UniqueMediaIdentifier { get; set; }
        [Required]
        public JObject JsonMetadata { get; set; } = new JObject();
        public string VideoId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Video Video { get; set; }
        public string PlaylistId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Playlist Playlist { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public virtual List<WatchHistory> WatchHistories { get; set; }
        public Visibility Visibility { get; set; }
        public PublishStatus PublishStatus { get; set; }
    }

    public class Transcription : Entity
    {
        [ForeignKey("File")]
        public string FileId { get; set; } // Webvtt file
        public virtual FileRecord File { get; set; }
        public virtual FileRecord SrtFile { get; set; }

        public string SrtFileId { get; set; } 
        public string Language { get; set; }
        public string Description { get; set; }
        public string VideoId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Video Video { get; set; }
        [SwaggerIgnore]
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
        public string? PhraseHints { get; set; } // null if not yet processed

        // Reported duration extracted from MediaInfo. The actual video/audio/caption streams duration could be less
        // Returns null if unknown
        // TimeSpan.Zero means a video that is actually zero seconds
        // See UpdateMediaProperties
        public virtual TimeSpan? Duration { get; set; }

        [Required]
        public JObject SceneData { get; set; } = new JObject();
        [Required]
        public JObject JsonMetadata { get; set; } = new JObject();
        [Required]
        // MediaInfo extracted from the video file
        public virtual JObject FileMediaInfo { get; set; } = new JObject();


        public virtual void UpdateMediaProperties()
        {
            Duration = null;
            try
            {
                string s = (string)FileMediaInfo["format"]["duration"];
                Duration = TimeSpan.FromSeconds(Convert.ToDouble(s));
            }
            catch (Exception)
            {
                // Could not extract duration. We won't log this
            }

        }
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

        public class TranscriptionStatusMessages
        {
            public static readonly string NOERROR = "NoError";
            public static readonly string TIMEOUT = "ServiceTimeout";
        };
    }

    public class CourseOffering : Entity
    {
        public string CourseId { get; set; }
        public string OfferingId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Course Course { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }

    }

    public class UserOffering : Entity
    {
        public string OfferingId { get; set; }
        public string ApplicationUserId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual ApplicationUser ApplicationUser { get; set; }
        public string IdentityRoleId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual IdentityRole IdentityRole { get; set; }
    }

    public class Log : Entity
    {
        public string UserId { get; set; }
        public string OfferingId { get; set; }
        public string MediaId { get; set; }
        public string EventType { get; set; }
        [Required]
        public JObject Json { get; set; } = new JObject();
    }

    public class EPub : Entity
    {
        public ResourceType SourceType { get; set; }
        public string SourceId { get; set; }
        public string Title { get; set; }
        public string Filename { get; set; }
        public string Language { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public Visibility Visibility { get; set; }
        public PublishStatus PublishStatus { get; set; }
        [Required]
        public JObject Cover { get; set; } = new JObject();
        public List<JObject> Chapters { get; set; }
    }

    public class WatchHistory : Entity
    {
        public string MediaId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual Media Media { get; set; }
        public string ApplicationUserId { get; set; }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public virtual ApplicationUser ApplicationUser { get; set; }
        [Required]
        public JObject Json { get; set; } = new JObject();
    }

    public class Dictionary : Entity
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public enum ResourceType
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)

        Offering = 0,
        Course = 1,
        Media = 2,
        Playlist = 3,
        EPub = 4
    }

    public enum Ack
    {
        // Since these are persisted in the database these integer values are immutable once assigned (hence explicit)

        Pending = 0,
        Seen = 1
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
        [Required]
        public JObject Payload { get; set; } = new JObject();
        public LogLevel LogLevel { get; set; }
        public Ack Ack { get; set; }
    }

    // TaskItem moved to its own file TaskItem.cs

    public class Image : Entity
    {
        public ResourceType SourceType { get; set; }
        public string SourceId { get; set; }
        [ForeignKey("ImageFile")]
        public string ImageFileId { get; set; }
        public virtual FileRecord ImageFile { get; set; }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class SwaggerIgnoreAttribute : Attribute
    {
    }

}