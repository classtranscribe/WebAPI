using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        Inactive,
        Deleted
    }

    public enum SourceType
    {
        Echo360,
        Youtube,
        Local
    }
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [IgnoreDataMember]
        public virtual List<UserOffering> UserOfferings { get; set; }
        [IgnoreDataMember]
        public virtual University University { get; set; }
    }


    public class Entity
    {
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
        public Status Status { get; set; }

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
        public string CourseName { get; set; }
        public string Description { get; set; }
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
        public virtual List<OfferingPlaylist> OfferingPlaylists { get; set; }
        [IgnoreDataMember]
        public virtual List<UserOffering> OfferingUsers { get; set; }
        public AccessTypes AccessType { get; set; }
    }

    public class Playlist : Entity
    {
        public SourceType SourceType { get; set; }
        public string PlaylistIdentifier { get; set; }
        [IgnoreDataMember]
        public virtual List<Media> Medias { get; set; }
        [IgnoreDataMember]
        public virtual List<OfferingPlaylist> OfferingPlaylists { get; set; }
    }

    public class Media : Entity
    {
        public SourceType SourceType { get; set; }
        public string UniqueMediaIdentifier { get; set; }
        public JObject JsonMetadata { get; set; }
        [IgnoreDataMember]
        public virtual List<Transcription> Transcriptions { get; set; }
        [IgnoreDataMember]
        public virtual List<Video> Videos { get; set; }
        public string PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; }
    }

    public class Transcription : Entity
    {
        public string Path { get; set; }
        public string Description { get; set; }
        public string MediaId { get; set; }
        [IgnoreDataMember]
        public virtual Media Media { get; set; }
    }

    public class Video : Entity
    {
        public string Video1Path { get; set; }
        public string Video2Path { get; set; }
        public string AudioPath { get; set; }

        public string Description { get; set; }
        public string MediaId { get; set; }
        [IgnoreDataMember]
        public virtual Media Media { get; set; }
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

    public class OfferingPlaylist : Entity
    {
        public string OfferingId { get; set; }
        public string PlaylistId { get; set; }
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
        [IgnoreDataMember]
        public virtual Playlist Playlist { get; set; }
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
}
