using ClassTranscribeDatabase.Models;
using Innofactor.EfCoreJsonValueConverter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{

    public class CTContextFactory : IDesignTimeDbContextFactory<CTDbContext>
    {
        public CTDbContext CreateDbContext(string[] args)
        {
            return CTDbContext.CreateDbContext();
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
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<CourseOffering> CourseOfferings { get; set; }
        public DbSet<UserOffering> UserOfferings { get; set; }
        public DbSet<FileRecord> FileRecords { get; set; }
        public DbSet<Caption> Captions { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<EPub> EPubs { get; set; }
        public DbSet<Dictionary> Dictionaries { get; set; }

        public static string ConnectionStringBuilder()
        {
            // Sample connection string -> Server=<POSTGRES_SERVER_NAME>;Port=5432;Database=<POSTGRES_DB_NAME>;User Id=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>;
            var configurations = CTDbContext.GetConfigurations();
            return "Server=" + configurations["POSTGRES_SERVER_NAME"] + ";Port=5432;Database="
                + configurations["POSTGRES_DB"] + ";User Id=" + configurations["ADMIN_USER_ID"] + ";Password=" + configurations["ADMIN_PASSWORD"] + ";";
        }

        public static DbContextOptionsBuilder<CTDbContext> GetDbContextOptionsBuilder()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>();
            optionsBuilder.UseLazyLoadingProxies().UseNpgsql(ConnectionStringBuilder(), npgsqlOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
            });
            return optionsBuilder;
        }
        public static CTDbContext CreateDbContext()
        {
            return new CTDbContext(GetDbContextOptionsBuilder().Options, null);
        }

        public static IConfiguration GetConfigurations()
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            if (configuration.GetValue<string>("DEV_ENV", "NULL") != "DOCKER")
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                configuration = new ConfigurationBuilder().SetBasePath(path).AddJsonFile("vs_appsettings.json").Build();
            }
            return configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionStringBuilder());
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
            builder.Entity<ApplicationUser>().HasQueryFilter(m => m.Status == Status.Active);
            builder.Entity<University>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Department>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Course>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Term>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Offering>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Media>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Transcription>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Video>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Playlist>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<CourseOffering>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<UserOffering>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<FileRecord>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Caption>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Log>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Dictionary>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);


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

            builder.Entity<Media>().HasOne(m => m.Video).WithMany(v => v.Medias).HasForeignKey(m => m.VideoId);

            builder.Entity<Playlist>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<Media>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<Log>().Property(m => m.Json).HasJsonValueConversion();
            builder.Entity<ApplicationUser>().Property(m => m.Metadata).HasJsonValueConversion();
            builder.Entity<Video>().Property(m => m.SceneData).HasJsonValueConversion();
            builder.Entity<Video>().Property(m => m.JsonMetadata).HasJsonValueConversion();
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
                            entity.IsDeletedStatus = Status.Active;
                            entity.CreatedAt = entity.CreatedAt != new DateTime() ? entity.CreatedAt : now;
                            entity.CreatedBy = user;
                            entity.LastUpdatedAt = now;
                            entity.LastUpdatedBy = user;
                            break;
                        case EntityState.Deleted:
                            entry.State = EntityState.Modified;
                            entity.IsDeletedStatus = Status.Deleted;
                            break;
                    }
                }
            }
        }

        private string GetCurrentUser()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null || user.FindFirst(ClaimTypes.NameIdentifier) == null)
            {
                return null;
            }
            else
            {
                return user.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }
    }
}
