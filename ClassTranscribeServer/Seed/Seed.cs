using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Seed
{
    public class Seeder
    {
        private readonly CTDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public Seeder(CTDbContext context, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        IdentityRole Instructor = new IdentityRole { Name = Globals.ROLE_INSTRUCTOR };
        IdentityRole Student = new IdentityRole { Name = Globals.ROLE_STUDENT };
        IdentityRole Admin = new IdentityRole { Name = Globals.ROLE_ADMIN };
        IdentityRole UniversityAdmin = new IdentityRole { Name = Globals.ROLE_UNIVERSITY_ADMIN };
        public async Task<Boolean> CreateRoles()
        {
            List<IdentityRole> roles = new List<IdentityRole> { Instructor, Student, Admin, UniversityAdmin };
            for (int i = 0; i < roles.Count(); i++)
            {

                if (!await _roleManager.RoleExistsAsync(roles[i].Name))
                {
                    await _roleManager.CreateAsync(roles[i]);
                }
                else
                {
                    roles[i].Id = await _roleManager.GetRoleIdAsync(roles[i]);
                }
            }
            return true;
        }

        public async Task<Boolean> SeedAsync()
        {

            University university1 = new University
            {
                // University Id begins with 1
                Id = "1001",
                Name = "UIUC",
                Domain = "illinois.edu"
                // Departments = { department1, department2 }
            };

            List<University> universities = new List<University> { university1 };

            foreach (var t in universities)
            {
                if (!_context.Universities.Contains(t))
                {
                    _context.Universities.Add(t);
                }
            }

            await _context.SaveChangesAsync();

            ApplicationUser user1 = new ApplicationUser
            {
                UserName = "instructor1",
                Email = "instructor1@test.edu",
                FirstName = "Instructor1",
                LastName = "Instructor",
                UniversityId = university1.Id
            };

            ApplicationUser user2 = new ApplicationUser
            {
                UserName = "instructor2",
                Email = "instructor2@test.edu",
                FirstName = "Instructor2",
                LastName = "Instructor",
                UniversityId = university1.Id
            };

            ApplicationUser user3 = new ApplicationUser
            {
                UserName = "instructor3",
                Email = "instructor3@test.edu",
                FirstName = "Instructor3",
                LastName = "Instructor",
                UniversityId = university1.Id
            };

            ApplicationUser user4 = new ApplicationUser
            {
                UserName = "student1",
                Email = "student1@test.edu",
                FirstName = "Student1",
                LastName = "Student",
                UniversityId = university1.Id
            };

            ApplicationUser user5 = new ApplicationUser
            {
                UserName = "student2",
                Email = "student2@test.edu",
                FirstName = "Student2",
                LastName = "Student",
                UniversityId = university1.Id
            };

            ApplicationUser user6 = new ApplicationUser
            {
                UserName = "ta",
                Email = "ta1@test.edu",
                FirstName = "TA1",
                LastName = "TA",
                UniversityId = university1.Id
            };

            ApplicationUser shawn = new ApplicationUser
            {
                UserName = "ruihua.sui@gmail.com",
                Email = "ruihua.sui@gmail.com",
                FirstName = "Ruihua",
                LastName = "Sui",
                UniversityId = university1.Id
            };

            ApplicationUser chirantan = new ApplicationUser
            {
                UserName = "mahipal2@illinois.edu",
                Email = "mahipal2@illinois.edu",
                FirstName = "Chirantan",
                LastName = "Mahipal",
                UniversityId = university1.Id
            };


            List<string> userIds = new List<string>();
            List<ApplicationUser> users = new List<ApplicationUser> { user1, user2, user3, user4, user5, user6, shawn, chirantan };
            foreach (ApplicationUser user in users)
            {
                var query = _userManager.Users.Where(u => u.Email == user.Email);
                if (query.Count() == 0)
                {
                    var res = await _userManager.CreateAsync(user, user.Email);
                    userIds.Add(user.Id);
                }
                else
                {
                    userIds.Add(query.First().Id);
                }
            }

            Boolean result = await CreateRoles();

            Term term1 = new Term
            {
                // Term Id begins with 0
                Id = "0001",
                Name = "Spring 2019",
                StartDate = new DateTime(2019, 1, 14, 0, 0, 0),
                EndDate = new DateTime(2019, 5, 14, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            Term term2 = new Term
            {
                // Term Id begins with 0
                Id = "0002",
                Name = "Fall 2018",
                StartDate = new DateTime(2018, 8, 27, 0, 0, 0),
                EndDate = new DateTime(2019, 1, 10, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            Term term3 = new Term
            {
                // Term Id begins with 0
                Id = "0003",
                Name = "Spring 2018",
                StartDate = new DateTime(2018, 1, 15, 0, 0, 0),
                EndDate = new DateTime(2018, 5, 14, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            term1.UniversityId = university1.Id;
            term2.UniversityId = university1.Id;
            term3.UniversityId = university1.Id;

            Department department1 = new Department
            {
                // department Id begins with 2
                Id = "2001",
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = university1.Id
            };

            Department department2 = new Department
            {
                // department Id begins with 2
                Id = "2002",
                Name = "Electrical and Computer Engineering",
                Acronym = "ECE",
                UniversityId = university1.Id
            };

            Department department3 = new Department
            {
                // department Id begins with 2
                Id = "2003",
                Name = "Math",
                Acronym = "MATH",
                UniversityId = university1.Id,
            };

            Course course1 = new Course
            {
                Id = "3001",
                CourseName = "Distributed Systems",
                CourseNumber = "425",
                DepartmentId = department1.Id
            };

            Course course2 = new Course
            {
                Id = "3002",
                CourseName = "Distributed Systems",
                CourseNumber = "428",
                DepartmentId = department2.Id,
                //CourseOfferings
            };

            Course course3 = new Course
            {
                Id = "3003",
                CourseName = "Computer Systems",
                CourseNumber = "233",
                DepartmentId = department1.Id,
                //CourseOfferings
            };

            Course course4 = new Course
            {
                Id = "3004",
                CourseName = "Calculus",
                CourseNumber = "220",
                DepartmentId = department3.Id,
                //CourseOfferings
            };

            Course course5 = new Course
            {
                Id = "3005",
                CourseName = "Applied Linear Algebra",
                CourseNumber = "415",
                DepartmentId = department3.Id,
                //CourseOfferings
            };

            Course course6 = new Course
            {
                Id = "3006",
                CourseName = "Numerical Methods",
                CourseNumber = "357",
                DepartmentId = department2.Id,
                //CourseOfferings
            };


            Offering offering1 = new Offering
            {
                Id = "4001",
                SectionName = "XY",
                TermId = term1.Id,
                AccessType = AccessTypes.Public
            };

            Offering offering2 = new Offering
            {
                Id = "4002",
                SectionName = "AB",
                TermId = term2.Id,
                AccessType = AccessTypes.AuthenticatedOnly
            };

            Offering offering3 = new Offering
            {
                Id = "4003",
                SectionName = "CD",
                TermId = term3.Id,
                AccessType = AccessTypes.StudentsOnly
            };

            Offering offering4 = new Offering
            {
                Id = "4004",
                SectionName = "EF",
                TermId = term1.Id,
                AccessType = AccessTypes.UniversityOnly
            };

            Offering offering5 = new Offering
            {
                Id = "4005",
                SectionName = "GH",
                TermId = term2.Id,
                AccessType = AccessTypes.Public
            };

            CourseOffering course_offering1 = new CourseOffering
            {
                CourseId = course1.Id,
                OfferingId = offering1.Id
            };

            CourseOffering course_offering2 = new CourseOffering
            {
                CourseId = course1.Id,
                OfferingId = offering2.Id
            };

            CourseOffering course_offering3 = new CourseOffering
            {
                CourseId = course1.Id,
                OfferingId = offering3.Id
            };

            CourseOffering course_offering4 = new CourseOffering
            {
                CourseId = course1.Id,
                OfferingId = offering4.Id
            };

            CourseOffering course_offering5 = new CourseOffering
            {
                CourseId = course1.Id,
                OfferingId = offering5.Id
            };

            CourseOffering course_offering6 = new CourseOffering
            {
                CourseId = course2.Id,
                OfferingId = offering1.Id
            };

            CourseOffering course_offering7 = new CourseOffering
            {
                CourseId = course2.Id,
                OfferingId = offering2.Id
            };

            CourseOffering course_offering8 = new CourseOffering
            {
                CourseId = course2.Id,
                OfferingId = offering3.Id
            };

            CourseOffering course_offering9 = new CourseOffering
            {
                CourseId = course2.Id,
                OfferingId = offering4.Id
            };

            CourseOffering course_offering10 = new CourseOffering
            {
                CourseId = course2.Id,
                OfferingId = offering5.Id
            };

            List<Term> terms = new List<Term> { term1, term2, term3 };
            List<Department> departments = new List<Department> { department1, department2, department3 };
            List<Course> courses = new List<Course> { course1, course2, course3, course4, course5, course6 };
            List<Offering> offerings = new List<Offering> { offering1, offering2, offering3, offering4, offering5 };
            List<CourseOffering> courseOfferings = new List<CourseOffering> { course_offering1, course_offering2, course_offering3, course_offering4,
                course_offering5, course_offering6, course_offering7, course_offering8, course_offering9, course_offering10 };

            foreach (var t in terms)
            {
                if (!_context.Terms.Contains(t))
                {
                    _context.Terms.Add(t);
                }
            }

            foreach (var t in departments)
            {
                if (!_context.Departments.Contains(t))
                {
                    _context.Departments.Add(t);
                }
            }

            foreach (var t in courses)
            {
                if (!_context.Courses.Contains(t))
                {
                    _context.Courses.Add(t);
                }
            }

            foreach (var t in offerings)
            {
                if (!_context.Offerings.Contains(t))
                {
                    _context.Offerings.Add(t);
                }
            }

            foreach (var t in courseOfferings)
            {
                if (_context.CourseOfferings.Where(u => u.OfferingId == t.OfferingId && u.CourseId == t.CourseId).Count() == 0)
                {
                    _context.CourseOfferings.Add(t);
                }
            }

            await _context.SaveChangesAsync();

            UserOffering userOffering1 = new UserOffering
            {
                OfferingId = offering1.Id,
                ApplicationUserId = userIds[0],
                IdentityRole = Instructor

            };

            UserOffering userOffering2 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = userIds[0],
                IdentityRole = Instructor
            };

            UserOffering userOffering3 = new UserOffering
            {
                OfferingId = offering3.Id,
                ApplicationUserId = userIds[1],
                IdentityRole = Instructor
            };

            UserOffering userOffering4 = new UserOffering
            {
                OfferingId = offering4.Id,
                ApplicationUserId = userIds[1],
                IdentityRole = Instructor
            };

            UserOffering userOffering5 = new UserOffering
            {
                OfferingId = offering1.Id,
                ApplicationUserId = userIds[3],
                IdentityRole = Student
            };

            UserOffering userOffering6 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = userIds[4],
                IdentityRole = Student
            };

            UserOffering sOffering1 = new UserOffering
            {
                OfferingId = offering1.Id,
                ApplicationUserId = userIds[6],
                IdentityRole = Instructor
            };

            UserOffering sOffering2 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = userIds[6],
                IdentityRole = Instructor
            };


            List<UserOffering> userOfferings = new List<UserOffering> { userOffering1, userOffering2, userOffering3, userOffering4, userOffering5, userOffering6, sOffering1, sOffering2 };

            foreach (var t in userOfferings)
            {
                if (_context.UserOfferings.Where(u => u.OfferingId == t.OfferingId && u.ApplicationUserId == t.ApplicationUserId && u.IdentityRole.Name == t.IdentityRole.Name).Count() == 0)
                {
                    _context.UserOfferings.Add(t);
                }
            }

            //Playlist echoPlaylist = new Playlist
            //{
            //    Id = "echo_sample",
            //    PlaylistIdentifier = "https://echo360.org/section/9d6e3b31-d3ac-4cfa-b44f-24c1a7c60fd5/public",
            //    SourceType = SourceType.Echo360
            //};

            Playlist youtubePlaylist = new Playlist
            {
                Id = "youtube_sample",
                PlaylistIdentifier = "PLLssT5z_DsK8Jk8mpFc_RPzn2obhotfDO",
                SourceType = SourceType.Youtube
            };

            List<Playlist> playlists = new List<Playlist> { youtubePlaylist };

            foreach (var t in playlists)
            {
                if (!_context.Playlists.Contains(t))
                {
                    _context.Playlists.Add(t);
                }
            }
            youtubePlaylist.OfferingId = offering2.Id;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
