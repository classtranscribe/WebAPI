using ClassTranscribeDatabase.Models;
using Innofactor.EfCoreJsonValueConverter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{

    public class CTContextFactory : IDesignTimeDbContextFactory<CTDbContext>
    {
        public CTDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>();
            optionsBuilder.UseNpgsql(CTDbContext.GetConfigurations()["POSTGRES"]);
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
        public DbSet<OfferingPlaylist> OfferingPlaylists { get; set; }
        public DbSet<UserOffering> UserOfferings { get; set; }

        public  static IConfiguration GetConfigurations()
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            if (configuration.GetValue<string>("DEV_ENV", "NULL") != "DOCKER")
            {
                configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("vs_appsettings.json").Build();
            }
            return configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {            
            optionsBuilder.UseNpgsql(CTDbContext.GetConfigurations()["POSTGRES"]);
        }

        public CTDbContext() { }

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


            builder.Entity<OfferingPlaylist>()
            .HasKey(t => new { t.OfferingId, t.PlaylistId });

            builder.Entity<OfferingPlaylist>()
                .HasOne(pt => pt.Offering)
                .WithMany(p => p.OfferingPlaylists)
                .HasForeignKey(pt => pt.OfferingId);

            builder.Entity<OfferingPlaylist>()
                .HasOne(pt => pt.Playlist)
                .WithMany(t => t.OfferingPlaylists)
                .HasForeignKey(pt => pt.PlaylistId);

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
            
            builder.Entity<Media>().Property(m => m.JsonMetadata).HasJsonValueConversion();
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
}
