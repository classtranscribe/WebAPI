using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClassTranscribeDatabase
{
    /// <summary>
    /// This class "Seeds" the database with some default values when a database in newly created.
    /// </summary>
    public class Seeder
    {
        private readonly CTDbContext _context;
        private readonly ILogger _logger;

        public Seeder(CTDbContext context, ILogger<Seeder> logger)
        {
            _context = context;
            _logger = logger;
        }
        public void Seed()
        {
            _logger.LogInformation("Database seeder starting");

            _logger.LogInformation($"Attempting connection to server {Globals.appSettings.POSTGRES_SERVER_NAME} for database {Globals.appSettings.POSTGRES_DB} using user {Globals.appSettings.ADMIN_USER_ID}");
            // This is where we first use the Database, so authentication and connection issues are likely to be discovered here.
            // Or maybe the database is just at restarting
            //
            int maxAttempts = 100;
            int retrySeconds = 15;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (_context.Database.CanConnect())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation( "Database.CanConnect() exception: {0}",ex.Message);
                    // https://www.postgresql.org/docs/9.4/errcodes-appendix.html
                    string ignore = "57P03"; // "57P03: the database system is starting up";

                    if (! ex.Message.Contains(ignore) ){
                        throw ex;
                    }
                }
                _logger.LogError($"Attempt {attempt} of {maxAttempts}. Cannot connect to Database");
                if (attempt < maxAttempts)
                {
                    _logger.LogInformation($"Sleeping for {retrySeconds} seconds");
                    Thread.Sleep(1000 * retrySeconds);
                }
                else
                {
                    _logger.LogError("Seed() Giving up on database :-( ");
                    throw new Exception("Could not connect to database");
                }
            }
            _logger.LogInformation("Migration starting");

            _context.Database.Migrate();

            _logger.LogInformation("Migration complete");
            _logger.LogInformation("Creating roles");

            IdentityRole Instructor = new IdentityRole { Name = Globals.ROLE_INSTRUCTOR, Id = "0000", NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper() };
            IdentityRole Student = new IdentityRole { Name = Globals.ROLE_STUDENT, Id = "0001", NormalizedName = Globals.ROLE_STUDENT.ToUpper() };
            IdentityRole Admin = new IdentityRole { Name = Globals.ROLE_ADMIN, Id = "0002", NormalizedName = Globals.ROLE_ADMIN.ToUpper() };
            IdentityRole UniversityAdmin = new IdentityRole { Name = Globals.ROLE_UNIVERSITY_ADMIN, Id = "0003", NormalizedName = Globals.ROLE_UNIVERSITY_ADMIN.ToUpper() };
            IdentityRole TeachingAssistant = new IdentityRole { Name = Globals.ROLE_TEACHING_ASSISTANT, Id = "0004", NormalizedName = Globals.ROLE_TEACHING_ASSISTANT.ToUpper() };
            IdentityRole Advisors = new IdentityRole { Name = Globals.ROLE_ADVISORS, Id = "0005", NormalizedName = Globals.ROLE_ADVISORS.ToUpper() };

            List<IdentityRole> roles = new List<IdentityRole> { Instructor, Student, Admin, UniversityAdmin, TeachingAssistant, Advisors };
            for (int i = 0; i < roles.Count(); i++)
            {
                if (!_context.Roles.IgnoreQueryFilters().Any(r => r.Name == roles[i].Name))
                {
                    _context.Roles.Add(roles[i]);
                }
            }

            University sampleUniversity = new University
            {
                // University Id begins with 1
                Id = "1001",
                Name = "ClassTranscribe Test University",
                Domain = "classtranscribe.com"
                // Departments = { department1, department2 }
            };

            University unknownUniversity = new University
            {
                // University Id begins with 1
                Id = "0000",
                Name = "Unknown",
                Domain = "UNK"
            };

            List<University> universities = new List<University> { sampleUniversity, unknownUniversity };

            foreach (var t in universities)
            {
                if (!_context.Universities.IgnoreQueryFilters().Contains(t))
                {
                    _context.Universities.Add(t);
                }
            }

            ApplicationUser testuser = new ApplicationUser
            {
                Id = "99",
                UserName = "testuser999@classtranscribe.com",
                Email = "testuser999@classtranscribe.com",
                FirstName = "Test",
                LastName = "User",
                UniversityId = sampleUniversity.Id,
                NormalizedEmail = "TESTUSER999@CLASSTRANSCRIBE.COM",
                NormalizedUserName = "TESTUSER999@CLASSTRANSCRIBE.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            testuser.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(testuser, testuser.Email);

            List<ApplicationUser> users = new List<ApplicationUser> { testuser };
            foreach (ApplicationUser user in users)
            {
                if (!_context.Users.IgnoreQueryFilters().Any(u => u.Email == user.Email))
                {
                    _context.Users.Add(user);
                    _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = Instructor.Id, UserId = user.Id });
                    _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = Admin.Id, UserId = user.Id });
                }
            }

            _context.SaveChanges();

            Term term1 = new Term
            {
                // Term Id begins with 0
                Id = "0001",
                Name = "Test Term",
                StartDate = DateTime.Now.AddMonths(-3),
                EndDate = DateTime.Now,
                // Offerings, University, UniversityId
            };

            term1.UniversityId = sampleUniversity.Id;

            Department department1 = new Department
            {
                // department Id begins with 2
                Id = "2001",
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = sampleUniversity.Id
            };

            Department department2 = new Department
            {
                // department Id begins with 2
                Id = "2002",
                Name = "Electrical and Computer Engineering",
                Acronym = "ECE",
                UniversityId = sampleUniversity.Id
            };

            Course test_course = new Course
            {
                Id = "test_course",
                CourseNumber = "000",
                DepartmentId = department1.Id
            };

            Offering offering2 = new Offering
            {
                Id = "4002",
                SectionName = "AB",
                CourseName = "Test Course",
                TermId = term1.Id,
                AccessType = AccessTypes.Public
            };

            CourseOffering course_offering2 = new CourseOffering
            {
                Id = "9002",
                CourseId = test_course.Id,
                OfferingId = offering2.Id
            };

            List<Term> terms = new List<Term> { term1 };
            List<Department> departments = new List<Department> { department1, department2 };
            List<Course> courses = new List<Course> { test_course };
            List<Offering> offerings = new List<Offering> { offering2 };
            List<CourseOffering> courseOfferings = new List<CourseOffering> { course_offering2 };

            foreach (var t in terms)
            {
                if (!_context.Terms.IgnoreQueryFilters().Contains(t))
                {
                    _context.Terms.Add(t);
                }
            }

            foreach (var t in departments)
            {
                if (!_context.Departments.IgnoreQueryFilters().Contains(t))
                {
                    _context.Departments.Add(t);
                }
            }

            foreach (var t in courses)
            {
                if (!_context.Courses.IgnoreQueryFilters().Contains(t))
                {
                    _context.Courses.Add(t);
                }
            }

            foreach (var t in offerings)
            {
                if (!_context.Offerings.IgnoreQueryFilters().Contains(t))
                {
                    _context.Offerings.Add(t);
                }
            }

            foreach (var t in courseOfferings)
            {
                if (_context.CourseOfferings.IgnoreQueryFilters().Where(u => u.OfferingId == t.OfferingId && u.CourseId == t.CourseId).Count() == 0)
                {
                    _context.CourseOfferings.Add(t);
                }
            }

            _context.SaveChanges();

            UserOffering userOffering2 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = users[0].Id,
                IdentityRoleId = Instructor.Id
            };

            List<UserOffering> userOfferings = new List<UserOffering> { userOffering2 };

            foreach (var t in userOfferings)
            {
                if (!_context.UserOfferings.IgnoreQueryFilters().Any(u => u.OfferingId == t.OfferingId && u.ApplicationUserId == t.ApplicationUserId && u.IdentityRoleId == t.IdentityRoleId))
                {
                    _context.UserOfferings.Add(t);
                }
            }

            _context.SaveChanges();

            Playlist youtubePlaylist = new Playlist
            {
                Id = "CT_Test_Playlist",
                PlaylistIdentifier = "PL9Q1c4uIAoWSc8_VV9JZSMDYdS5xzMWtU",
                SourceType = SourceType.Youtube,
                Name = "Youtube Sample"
            };

            List<Playlist> playlists = new List<Playlist> { youtubePlaylist };

            foreach (var t in playlists)
            {
                if (!_context.Playlists.IgnoreQueryFilters().Contains(t))
                {
                    _context.Playlists.Add(t);
                }
            }
            youtubePlaylist.OfferingId = offering2.Id;

            _context.SaveChanges();
            _logger.LogInformation("Seeded");
        }
    }
}
