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

namespace ClassTranscribeServer.Controllers
{
    [Route("[controller]/[action]")]
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
                return GenerateJwtToken(user.Email, appUser);
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
                return GenerateJwtToken(user.Email, user);
            }

            throw new ApplicationException("UNKNOWN_ERROR");
        }

        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> TestSignIn([FromBody] TestLoginDTO model)
        {
            LoggedInDTO loggedInDTO;
            try
            {
                ApplicationUser user = await _userManager.FindByEmailAsync(model.emailId);
                await _signInManager.SignInAsync(user, false);
                loggedInDTO = GenerateJwtToken(user.Email, user);
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
                ApplicationUser user = Validate(model.b2cToken);
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
            catch (Exception)
            {
                return Unauthorized();
            }

            return Ok(loggedInDTO);
        }

        [NonAction]
        private LoggedInDTO GenerateJwtToken(string email, ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

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
        public ApplicationUser Validate(string token)
        {
            string stsDiscoveryEndpoint = "https://" + Globals.appSettings.AZURE_B2C_DOMAIN + "/" + Globals.appSettings.AZURE_B2C_DIRECTORY + "/v2.0/.well-known/openid-configuration?p=" + Globals.appSettings.AZURE_B2C_SIGNIN_POLICY;

            ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());

            OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = Globals.appSettings.AZURE_B2C_CLIENTID,
                ValidateIssuer = false,
                ValidIssuer = config.Issuer,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = false
            };


            JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

            SecurityToken jwt;

            var claims = tokendHandler.ValidateToken(token, validationParameters, out jwt);

            JwtSecurityToken j = jwt as JwtSecurityToken;

            var user = new ApplicationUser
            {
                UserName = claims.FindFirst("emails")?.Value,
                Email = claims.FindFirst("emails")?.Value,
                FirstName = claims.FindFirst("given_name")?.Value,
                LastName = claims.FindFirst("family_name")?.Value
            };

            return user;
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
            using (StreamReader r = new StreamReader("world_universities_and_domains.json"))
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
            public string b2cToken { get; set; }
        }

        public class TestLoginDTO
        {
            public string emailId { get; set; }
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