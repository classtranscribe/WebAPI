using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

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
        Local
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
        public Status IsDeletedStatus { get; set; }

    }
    public class FileRecord : Entity
    {
        public FileRecord(string path)
        {
            char separator = System.IO.Path.DirectorySeparatorChar;
            this.Path = path;
            this.FileName = path.Substring(path.LastIndexOf(separator) + 1);
            this.Hash = ComputeSha256HashForFile(this.Path);
        }
        [NotMapped]
        public string FileName { get; set; }
        private string path;
        public string Path
        {
            get
            {
                string p = path;
                // Windows
                if (System.IO.Path.DirectorySeparatorChar == '\\')
                {
                    p = path.Replace('\\', '/');
                }
                p = p.Substring(p.LastIndexOf("/Data/") + 6);
                return System.IO.Path.Combine(CTDbContext.appSettings.DATA_DIRECTORY, p);
            }
            set
            {
                // Windows
                if (value.Contains('\\'))
                {
                    value = value.Replace('\\', '/');
                }
                path = value.Substring(value.LastIndexOf("/Data/"));
            }
        }
        public string VMPath
        {
            get
            {
                return path;
            }
        }
        public string Hash { get; set; }

        public static string ComputeSha256HashForFile(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                // Create a SHA256
                SHA256 Sha256 = SHA256.Create();

                // ComputeHash - returns byte array  
                byte[] bytes = Sha256.ComputeHash(stream);
                // Convert byte array to a string   

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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
        public virtual List<Playlist> Playlists { get; set; }
        [IgnoreDataMember]
        public virtual List<UserOffering> OfferingUsers { get; set; }
        public AccessTypes AccessType { get; set; }
    }

    public class Playlist : Entity
    {
        public string Name { get; set; }
        public SourceType SourceType { get; set; }
        public string PlaylistIdentifier { get; set; }
        [IgnoreDataMember]
        public virtual List<Media> Medias { get; set; }
        public string OfferingId { get; set; }
        [IgnoreDataMember]
        public virtual Offering Offering { get; set; }
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
        [ForeignKey("File")]
        public string FileId { get; set; }
        public virtual FileRecord File { get; set; }

        string Description { get; set; }
        public string MediaId { get; set; }
        [IgnoreDataMember]
        public virtual Media Media { get; set; }
    }

    public class Video : Entity
    {
        [ForeignKey("Video1")]
        public string Video1Id { get; set; }
        
        public virtual FileRecord Video1 { get; set; }

        [ForeignKey("Video2")]
        public string Video2Id { get; set; }
        public virtual FileRecord Video2 { get; set; }

        [ForeignKey("Audio")]
        public string AudioId { get; set; }
        public virtual FileRecord Audio { get; set; }

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
