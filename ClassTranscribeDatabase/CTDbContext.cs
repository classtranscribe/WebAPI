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

    /// <summary>
    /// This class is the primary class responsible for all the interactions with the database.
    /// For general info on DbContext - https://www.entityframeworktutorial.net/entityframework6/dbcontext.aspx
    /// Each member of this class corresponds to a Database Table.
    /// 
    /// Steps to add a new entity/table to the database.
    /// 1. Define the model class under Models.cs, ensure this model class is derived from "Entity".
    /// 2. Add a DbSet member variable to CTDbContext for this new model.
    /// 3. Add the queryfilter statement under OnModelCreating()
    /// 4. If there are any m-to-n relationships define them under OnModelCreating(), more info, https://www.learnentityframeworkcore.com/configuration/many-to-many-relationship-configuration
    /// 5. If there are any JObject properties in the new model, add the HasJsonValueConversion() for that property,
    /// see OnModelCreating() for other models having such properties.
    /// 6. Define any other model modifications using fluentAPI under OnModelCreating(), more info, https://www.learnentityframeworkcore.com/configuration/fluent-api
    /// </summary>
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
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<EPubChapter> EPubChapters { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        /// <summary>
        /// This method builds a connectionstring to connect with the database.
        /// More info, https://www.learnentityframeworkcore.com/connection-strings
        /// </summary>        
        public static string ConnectionStringBuilder()
        {
            // Sample connection string -> Server=<POSTGRES_SERVER_NAME>;Port=5432;Database=<POSTGRES_DB_NAME>;User Id=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>;
            var configurations = CTDbContext.GetConfigurations();
            return "Server=" + configurations["POSTGRES_SERVER_NAME"] + ";Port=5432;Database="
                + configurations["POSTGRES_DB"] + ";User Id=" + configurations["ADMIN_USER_ID"] + ";Password=" + configurations["ADMIN_PASSWORD"] + ";MaxPoolSize=1000;";
        }

        /// <summary>
        /// Additional options for configuing the database itself are defined here.
        /// For more info, https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// The configurations are key-value pairs that are either read in from 
        ///  - environment variables if this application is deployed via docker on a server.
        ///  - or from vs_appsettings.json file if the application is executed directly from using Visual Studio.
        /// </summary>
        /// <returns> The configurations </returns>
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

        /// <summary>
        /// These are additional modifications made in setting up the various tables of the database.
        /// Things like many-to-many relationships and queryfilters and alternative keys are defined here.
        /// These options are defined using the fluentAPI,
        /// For more info on fluentAPI - https://www.entityframeworktutorial.net/efcore/fluent-api-in-entity-framework-core.aspx
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            
            // This query filter is added to facilitate the soft-delete feature.
            // Soft-Delete implies that know row is actually every deleted, just their "Status" columns are tagged
            // as inactive.
            // This query filter is added so that every query made has a default where clause added to them,
            // i.e. to search only the "Active" rows of the table.
            // For more info, google "Soft Delete EntityFramework Core"
            //
            // A similar statement must be added for every new table added.
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
            builder.Entity<WatchHistory>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Subscription>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<Message>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<EPubChapter>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<EPub>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);
            builder.Entity<TaskItem>().HasQueryFilter(m => m.IsDeletedStatus == Status.Active);

            // Configure m-to-n relationships.
            builder.Entity<CourseOffering>()
            .HasIndex(t => new { t.CourseId, t.OfferingId, t.DeletedAt }).IsUnique();

            builder.Entity<CourseOffering>()
                .HasOne(pt => pt.Course)
                .WithMany(p => p.CourseOfferings)
                .HasForeignKey(pt => pt.CourseId);

            builder.Entity<CourseOffering>()
                .HasOne(pt => pt.Offering)
                .WithMany(t => t.CourseOfferings)
                .HasForeignKey(pt => pt.OfferingId);

            builder.Entity<UserOffering>()
            .HasIndex(t => new { t.ApplicationUserId, t.OfferingId, t.IdentityRoleId, t.DeletedAt }).IsUnique();

            builder.Entity<UserOffering>()
                .HasOne(pt => pt.Offering)
                .WithMany(p => p.OfferingUsers)
                .HasForeignKey(pt => pt.OfferingId);

            builder.Entity<UserOffering>()
                .HasOne(pt => pt.ApplicationUser)
                .WithMany(t => t.UserOfferings)
                .HasForeignKey(pt => pt.ApplicationUserId);

            builder.Entity<Media>().HasOne(m => m.Video)
                .WithMany(v => v.Medias)
                .HasForeignKey(m => m.VideoId);

            // Configure Entities which have a JObject.
            builder.Entity<Playlist>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<Media>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<Log>().Property(m => m.Json).HasJsonValueConversion();
            builder.Entity<ApplicationUser>().Property(m => m.Metadata).HasJsonValueConversion();
            builder.Entity<Video>().Property(m => m.SceneData).HasJsonValueConversion();
            builder.Entity<Video>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<Offering>().Property(m => m.JsonMetadata).HasJsonValueConversion();
            builder.Entity<WatchHistory>().Property(m => m.Json).HasJsonValueConversion();
            builder.Entity<Message>().Property(m => m.Payload).HasJsonValueConversion();
            builder.Entity<EPub>().Property(m => m.Json).HasJsonValueConversion();
            builder.Entity<EPubChapter>().Property(m => m.Data).HasJsonValueConversion();
            builder.Entity<TaskItem>().Property(m => m.TaskParameters).HasJsonValueConversion();
            builder.Entity<TaskItem>().Property(m => m.ResultData).HasJsonValueConversion();

            builder.Entity<Subscription>().HasIndex(s => new { s.ResourceType, s.ResourceId, s.ApplicationUserId, s.DeletedAt }).IsUnique();
            builder.Entity<TaskItem>().HasIndex(t => new { t.UniqueId, t.TaskType, t.DeletedAt }).IsUnique();
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
                            entity.DeletedAt = now;
                            break;
                    }
                }
            }
        }

        private string GetCurrentUser()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null || user.FindFirst(Globals.CLAIM_USER_ID) == null)
            {
                return null;
            }
            else
            {
                return user.FindFirst(Globals.CLAIM_USER_ID).Value;
            }
        }
    }
}
