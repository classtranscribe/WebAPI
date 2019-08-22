using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ClassTranscribeDatabase;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Reflection;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CTDbContext _context;
        
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CTDbContext context
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [NonAction]
        public async Task<LoggedInDTO> Login(ApplicationUser user)
        {

            var result = await _signInManager.PasswordSignInAsync(user.Email, user.Email, false, false);

            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == user.Email);
                return await GenerateJwtToken(user.Email, appUser);
            }

            throw new ApplicationException("INVALID_LOGIN_ATTEMPT");
        }

        [NonAction]
        public async Task<LoggedInDTO> Register(ApplicationUser user)
        {
            var result = await _userManager.CreateAsync(user, user.Email);
            University university = await GetUniversity(user.Email);
            user.University = university;
            await _context.SaveChangesAsync();

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return await GenerateJwtToken(user.Email, user);
            }

            throw new ApplicationException("UNKNOWN_ERROR");
        }

        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> TestSignIn([FromBody] TestLoginDTO model)
        {
            if(model.password != Globals.appSettings.GOD_MODE_PASSWORD)
            {
                return Unauthorized();
            }
            LoggedInDTO loggedInDTO;
            try
            {
                ApplicationUser user = await _userManager.FindByEmailAsync(model.emailId);
                await _signInManager.SignInAsync(user, false);
                loggedInDTO = await GenerateJwtToken(user.Email, user);
            }
            catch (Exception)
            {
                return Unauthorized();
            }

            return Ok(loggedInDTO);
        }

        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> SignIn([FromBody] LoginDto model)
        {
            LoggedInDTO loggedInDTO;
            try
            {
                ApplicationUser user = await Validate(model.auth0Token);
                ApplicationUser applicationUser = await _userManager.FindByEmailAsync(user.Email);
                if (applicationUser == null)
                {
                    loggedInDTO = await Register(user);
                }
                else
                {
                    loggedInDTO = await Login(user);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return Unauthorized();
            }

            return Ok(loggedInDTO);
        }

        [NonAction]
        private async Task<LoggedInDTO> GenerateJwtToken(string email, ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };
            foreach(var role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.appSettings.JWT_KEY));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(Globals.appSettings.JWT_EXPIRE_DAYS));

            var token = new JwtSecurityToken(
                Globals.appSettings.JWT_ISSUER,
                Globals.appSettings.JWT_ISSUER,
                claims,
                expires: expires,
                signingCredentials: creds
            );
            return new LoggedInDTO
            {
                AuthToken = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                EmailId = email,
                UniversityId = user.UniversityId
            };
        }

        [NonAction]
        public async Task<ApplicationUser> Validate(string token)
        {
            string auth0Domain = Globals.appSettings.AUTH0_DOMAIN; // Your Auth0 domain
            string auth0Audience = Globals.appSettings.AUTH0_CLIENT_ID; // Your API Identifier

            IConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{auth0Domain}.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            TokenValidationParameters validationParameters =
            new TokenValidationParameters
            {
                ValidIssuer = auth0Domain,
                ValidAudiences = new[] { auth0Audience },
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };
            SecurityToken validatedToken;
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            var claims = handler.ValidateToken(token, validationParameters, out validatedToken);

            var applicationUser = new ApplicationUser
            {
                UserName = claims.FindFirstValue("email"),
                Email = claims.FindFirstValue("email"),
                FirstName = claims.FindFirstValue("given_name"),
                LastName = claims.FindFirstValue("family_name")
            };

            return applicationUser;
        }

        [NonAction]
        public async Task<University> GetUniversity(string mailId)
        {
            string domain = mailId.Split('@')[1];
            University university;
            if(_context.Universities.Where(u => u.Domain == domain).Any())
            {
                university = _context.Universities.Where(u => u.Domain == domain).Single();
            }
            else
            {
                string universityName = GetUniversityName(domain);
                if(universityName == "")
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
        [NonAction]
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
        public class LoginDto
        {
            [Required]
            public string auth0Token { get; set; }
        }

        public class TestLoginDTO
        {
            public string emailId { get; set; }
            public string password { get; set; }
        }
        public class LoggedInDTO
        {
            public string UserId { get; set; }
            public string UniversityId { get; set; }
            public string AuthToken { get; set; }
            public string EmailId { get; set; }
        }

    }
}