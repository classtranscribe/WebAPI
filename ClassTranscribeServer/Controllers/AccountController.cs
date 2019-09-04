using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
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
using Microsoft.AspNetCore.Authorization;
using ClassTranscribeServer.Utils;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CTDbContext _context;
        private readonly UserUtils _userUtils;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CTDbContext context
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _userUtils = new UserUtils(_userManager, _context);
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

        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult> CreateUser(string emailId)
        {
            ApplicationUser applicationUser = await _userManager.FindByEmailAsync(emailId);
            if (applicationUser != null)
            {
                return Ok("User exists.");
            }
            await _userUtils.CreateNonExistentUser(emailId);
            return Ok();
        }

        [NonAction]
        public async Task<LoggedInDTO> Register(ApplicationUser user)
        {
            var result = await _userManager.CreateAsync(user, user.Email);
            University university = await _userUtils.GetUniversity(user.Email);
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

        [HttpGet("GetUserMetadata")]
        [Authorize]
        public async Task<ActionResult<JObject>> GetUserMetadata()
        {
            ApplicationUser user = null;
            if (User.Identity.IsAuthenticated && this.User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                var userId = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = await _context.Users.FindAsync(userId);
                return user.Metadata;
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("PostUserMetadata")]
        [Authorize]
        public async Task<ActionResult> PostUserMetadata(JObject metadata)
        {
            ApplicationUser user = null;
            if (User.Identity.IsAuthenticated && this.User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                var userId = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = await _context.Users.FindAsync(userId);
                user.Metadata = metadata;
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return BadRequest();
            }
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
                    if (!(await _userManager.IsEmailConfirmedAsync(applicationUser)))
                    {
                        applicationUser.EmailConfirmed = true;
                        applicationUser.FirstName = user.FirstName;
                        applicationUser.LastName = user.LastName;
                        await _context.SaveChangesAsync();
                    }
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
                UniversityId = user.UniversityId,
                Metadata = user.Metadata
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
                LastName = claims.FindFirstValue("family_name"),
                EmailConfirmed = true
            };

            return applicationUser;
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
            public JObject Metadata { get; set; }
        }

    }
}