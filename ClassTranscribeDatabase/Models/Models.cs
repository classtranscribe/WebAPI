using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
}
