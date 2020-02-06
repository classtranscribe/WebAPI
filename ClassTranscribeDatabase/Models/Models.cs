using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Models
{
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


    public class Entity
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
    }

    public class Dictionary : Entity
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
