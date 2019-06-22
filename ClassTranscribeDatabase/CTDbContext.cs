using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ClassTranscribeDatabase
{

    public class CTContextFactory : IDesignTimeDbContextFactory<CTDbContext>
    {
        public CTDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            if(configuration.GetValue<string>("DEV_ENV", "NULL") != "DOCKER")
            {
                configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("vs_appsettings.json").Build();
            }
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>();
            optionsBuilder.UseNpgsql(configuration["POSTGRES"]);
            return new CTDbContext(optionsBuilder.Options, null);
        }
    }

    public class CTDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DbSet<University> Universities { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Offering> Offerings { get; set; }
        public DbSet<Media> Medias { get; set; }
        public DbSet<Transcription> Transcriptions { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<CourseOffering> CourseOfferings { get; set; }
        public DbSet<OfferingMedia> OfferingMedias { get; set; }
        public DbSet<UserOffering> UserOfferings { get; set; }


        public CTDbContext(DbContextOptions<CTDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<CourseOffering>()
            .HasKey(t => new { t.CourseId, t.OfferingId });

            builder.Entity<CourseOffering>()
                .HasOne(pt => pt.Course)
                .WithMany(p => p.CourseOfferings)
                .HasForeignKey(pt => pt.CourseId);

            builder.Entity<CourseOffering>()
                .HasOne(pt => pt.Offering)
                .WithMany(t => t.CourseOfferings)
                .HasForeignKey(pt => pt.OfferingId);


            builder.Entity<OfferingMedia>()
            .HasKey(t => new { t.OfferingId, t.MediaId });

            builder.Entity<OfferingMedia>()
                .HasOne(pt => pt.Offering)
                .WithMany(p => p.OfferingMedias)
                .HasForeignKey(pt => pt.OfferingId);

            builder.Entity<OfferingMedia>()
                .HasOne(pt => pt.Media)
                .WithMany(t => t.OfferingMedias)
                .HasForeignKey(pt => pt.MediaId);

            builder.Entity<UserOffering>()
            .HasKey(t => new { t.ApplicationUserId, t.OfferingId });

            builder.Entity<UserOffering>()
                .HasOne(pt => pt.Offering)
                .WithMany(p => p.OfferingUsers)
                .HasForeignKey(pt => pt.OfferingId);

            builder.Entity<UserOffering>()
                .HasOne(pt => pt.ApplicationUser)
                .WithMany(t => t.UserOfferings)
                .HasForeignKey(pt => pt.ApplicationUserId);
        }
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is Entity entity)
                {
                    var now = DateTime.UtcNow;
                    var user = GetCurrentUser();
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entity.LastUpdatedAt = now;
                            entity.LastUpdatedBy = user;
                            break;

                        case EntityState.Added:
                            entity.CreatedAt = now;
                            entity.CreatedBy = user;
                            entity.LastUpdatedAt = now;
                            entity.LastUpdatedBy = user;
                            break;
                    }
                }
            }
        }

        private string GetCurrentUser()
        {
            // TODO: Fix this
            return null;
            var httpContextAccessor = _httpContextAccessor;
            if (httpContextAccessor.HttpContext.User != null)
            {
                var httpContext = httpContextAccessor.HttpContext;
                var authenticatedUserName = httpContext.User.Identity.Name;

                // If it returns null, even when the user was authenticated, you may try to get the value of a specific claim 
                var authenticatedUserId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value ?? "";
                // var authenticatedUserId = _httpContextAccessor.HttpContext.User.FindFirst("sub").Value

                // TODO use name to set the shadow property value like in the following post: https://www.meziantou.net/2017/07/03/entity-framework-core-generate-tracking-columns
                return authenticatedUserId;
            }
            return null;
        }
    }

    public class Entity
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string LastUpdatedBy { get; set; }
    }

    public class University : Entity
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public List<Department> Departments { get; set; }
    }

    public class Department : Entity
    {
        public string Name { get; set; }
        public string Acronym { get; set; }
        public List<Course> Courses { get; set; }
        public string UniversityId { get; set; }
        public University University { get; set; }
    }

    public class Course : Entity
    {
        public string CourseNumber { get; set; }
        public string Description { get; set; }
        public string DepartmentId { get; set; }
        public Department Department { get; set; }
        public List<CourseOffering> CourseOfferings { get; set; }
    }

    public class Term : Entity
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public List<Offering> Offerings { get; set; }
    }

    public enum AccessTypes
    {
        Public,
        AuthenticatedOnly,
        StudentsOnly,
        UniversityOnly,
    }
    public class Offering : Entity
    {
        public string SectionName { get; set; }
        public string TermId { get; set; }
        public Term Term { get; set; }
        public List<CourseOffering> CourseOfferings { get; set; }
        public List<OfferingMedia> OfferingMedias { get; set; }
        public List<UserOffering> OfferingUsers { get; set; }
        public AccessTypes AccessType { get; set; }
    }

    public class Media : Entity
    {
        public string MediaSource { get; set; }
        public string MediaUrl { get; set; }
        // TODO: convert to JSON Object
        public string JsonMetadata { get; set; }
        public List<Transcription> Transcriptions { get; set; }
        public List<Video> Videos { get; set; }
        public List<OfferingMedia> OfferingMedias { get; set; }
    }

    public class Transcription : Entity
    {
        public string Path { get; set; }
        public string Description { get; set; }
        public string MediaId { get; set; }
        public Media Media { get; set; }
    }

    public class Video : Entity
    {
        public string Path { get; set; }
        public string Description { get; set; }
        public string MediaId { get; set; }
        public Media Media { get; set; }
    }

    public class CourseOffering : Entity
    {
        public string CourseId { get; set; }
        public string OfferingId { get; set; }
        public Course Course { get; set; }
        public Offering Offering { get; set; }

    }

    public class OfferingMedia : Entity
    {
        public string OfferingId { get; set; }
        public string MediaId { get; set; }
        public Offering Offering { get; set; }
        public Media Media { get; set; }
    }

    public class UserOffering : Entity
    {
        public string OfferingId { get; set; }
        public string ApplicationUserId { get; set; }
        public Offering Offering { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public string IdentityRoleId { get; set; }
        public IdentityRole IdentityRole { get; set; }

    }

}
