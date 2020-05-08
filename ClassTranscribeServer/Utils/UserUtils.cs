using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Utils
{
    public class UserUtils
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CTDbContext _context;

        public UserUtils(
            UserManager<ApplicationUser> userManager,
            CTDbContext context
            )
        {
            _userManager = userManager;
            _context = context;
        }
        public async Task<ApplicationUser> CreateNonExistentUser(string emailId)
        {
            ApplicationUser user = new ApplicationUser
            {
                EmailConfirmed = false,
                UserName = emailId,
                Email = emailId
            };
            var result = await _userManager.CreateAsync(user, user.Email);
            University university = await GetUniversity(user.Email);
            user.University = university;
            await _context.SaveChangesAsync();
            return await _userManager.FindByEmailAsync(emailId);
        }

        public async Task<University> GetUniversity(string mailId)
        {
            string domain = mailId.Split('@')[1];
            University university;
            if (_context.Universities.Where(u => u.Domain == domain).Any())
            {
                university = _context.Universities.Where(u => u.Domain == domain).Single();
            }
            else
            {
                string universityName = GetUniversityName(domain);
                if (universityName == "")
                {
                    // If domain is unknown return Special University as Unknown
                    university = await _context.Universities.FindAsync("0000");
                }
                else
                {
                    university = new University
                    {
                        Name = universityName,
                        Domain = domain
                    };
                    await _context.Universities.AddAsync(university);
                    await _context.SaveChangesAsync();
                }
            }
            return university;
        }
        public static string GetUniversityName(string domain)
        {
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(basePath, "world_universities_and_domains.json");
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                JArray allUniversities = JArray.Parse(json);
                if (allUniversities.Where(u => u["domains"].First().ToString() == domain).Any())
                {
                    return allUniversities.Where(u => u["domains"].First().ToString() == domain)
                    .First()["name"].ToString();
                }
                else
                {
                    return "";
                }

            }
        }

        public async Task<ApplicationUser> GetUser(ClaimsPrincipal claims)
        {
            if (claims != null && claims.Identity.IsAuthenticated && claims.FindFirst(Globals.CLAIM_USER_ID) != null)
            {
                var currentUserID = claims.FindFirst(Globals.CLAIM_USER_ID).Value;
                return await _context.Users.FindAsync(currentUserID);
            }
            else
            {
                return null;
            }
        }
    }

}
