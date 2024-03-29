﻿using Azure.Core;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]/[action]")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUtils _userUtils;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CTDbContext context, UserUtils userUtils,
            ILogger<AccountController> logger) : base(context, logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userUtils = userUtils;
        }

        [NonAction]
        public async Task<LoggedInDTO> Login(ApplicationUser user)
        {
            if (user == null)
            {
                throw new ApplicationException("INVALID_LOGIN_ATTEMPT");
            }

            var result = await _signInManager.PasswordSignInAsync(user.Email, user.Email, false, false);

            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == user.Email);
                return await GenerateJwtToken(appUser);
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
            if (user == null)
            {
                throw new ApplicationException("INVALID_REGISTRATION_ATTEMPT");
            }

            var result = await _userManager.CreateAsync(user, user.Email);
            University university = await _userUtils.GetUniversity(user.Email);
            user.University = university;
            await _context.SaveChangesAsync();

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return await GenerateJwtToken(user);
            }

            throw new ApplicationException("UNKNOWN_ERROR");
        }

        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<LoggedInDTO>> LoginAs([FromBody] LoginAsDTO model)
        {
            if (model == null)
            {
                return BadRequest();
            }
            LoggedInDTO loggedInDTO;
            try
            {
                ApplicationUser user = await _userManager.FindByEmailAsync(model.emailId);
                await _signInManager.SignInAsync(user, false);
                loggedInDTO = await GenerateJwtToken(user);
            }
            catch (Exception)
            {
                return Unauthorized();
                throw;
            }

            return Ok(loggedInDTO);
        }

        [HttpGet]
        public async Task<ActionResult<LoggedInDTO>> TestSignIn()
        {
            LoggedInDTO loggedInDTO;
            try
            {
                if (Globals.appSettings.TEST_SIGN_IN == "true")
                {
                    ApplicationUser user = await _userManager.FindByEmailAsync("testuser999@classtranscribe.com");
                    await _signInManager.SignInAsync(user, false);
                    loggedInDTO = await GenerateJwtToken(user);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception)
            {
                return Unauthorized();
            }

            return Ok(loggedInDTO);
        }

        public class MediaWorkerLoginDTO
        {
            public string Access { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> MediaWorkerSignIn(MediaWorkerLoginDTO accessDTO)
        {
            if (accessDTO == null) return BadRequest("Missing parameter");
            try
            {
                string access = accessDTO.Access;
                if (! string.IsNullOrEmpty(Globals.appSettings.MEDIA_WORKER_SHARED_SECRET) && Globals.appSettings.MEDIA_WORKER_SHARED_SECRET == access)
                {
                    ApplicationUser user = await _userManager.FindByEmailAsync(Globals.MEDIA_WORKER_EMAIL);
                    await _signInManager.SignInAsync(user, false);
                    LoggedInDTO loggedInDTO = await GenerateJwtToken(user);
                    return Ok(loggedInDTO);
                }
                else
                {
                    _logger.LogError( "TaskWorkerSignIn incorrect access secret or secret not set");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TaskWorkerSignIn failed");
            }

            return Unauthorized();
        }

        [HttpGet("GetUserMetadata")]
        [Authorize]
        public async Task<ActionResult<JObject>> GetUserMetadata()
        {
            ApplicationUser user = await _userUtils.GetUser(User);
            if (user != null)
            {
                return user.Metadata;
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("PostUserMetadata")]
        [Authorize]
        public async Task<ActionResult> PostUserMetadata([FromBody]JObject metadata)
        {
            var user = await _userUtils.GetUser(User);
            if (user != null)
            {
                user.Metadata = metadata ?? new JObject();
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
        #nullable enable
        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> LTISignIn(string sharedsecret, string email, string? first, string? last) {
            _logger.LogInformation("LTISignIn for {0}: starting", email);
            if( string.IsNullOrEmpty( Globals.appSettings.LTI_SHARED_SECRET)) {
                _logger.LogError("LTI_SHARED_SECRET environment variable not set thus LTI logins are not authorized ");
                 return Unauthorized("Server LTI Secret not set");
            }
            try {
                var allowed =  Globals.appSettings.LTI_SHARED_SECRET == sharedsecret;
                if(!allowed) {
                    return Unauthorized();
                }
                 _logger.LogInformation("LTISignIn for {0}", email);
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = first ?? "",
                    LastName = last ?? "",
                    EmailConfirmed = true
                };

                ApplicationUser applicationUser = await _userManager.FindByEmailAsync(email);

                LoggedInDTO loggedInDTO;
                if (applicationUser == null)
                {
                     _logger.LogInformation("LTISignIn for {0}: Register...", email);
                    loggedInDTO = await Register(user);
                }
                else
                {
                    if (!(await _userManager.IsEmailConfirmedAsync(applicationUser)))
                    {
                         _logger.LogInformation("LTISignIn for {0}:  Saving First and Last", email);
                        applicationUser.EmailConfirmed = true;
                        applicationUser.FirstName = user.FirstName;
                        applicationUser.LastName = user.LastName;
                        await _context.SaveChangesAsync();
                    }
                     _logger.LogInformation("LTISignIn for {0}:  Login...", email);
                    loggedInDTO = await Login(user);
                }
              
                _logger.LogInformation("LTISignIn for {0}: Returning okay", email);
                return Ok(loggedInDTO);
           }
           catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing in User with email {0}.", email);
                return Unauthorized();
            }
        } 
        #nullable disable

        [HttpPost]
        public async Task<ActionResult<LoggedInDTO>> SignIn([FromBody] LoginDto model)
        {
            if (model == null)
            {
                return Unauthorized();
            }

            LoggedInDTO loggedInDTO;
            try
            {
                ApplicationUser user = null;
                switch (model.AuthMethod)
                {
                    case AuthMethod.Auth0: user = await ValidateAuth0IDToken(model.Token); break;
                    case AuthMethod.CILogon: user = await ValidateCILogonAuthCode(model.Token, model.CallbackURL); break;
                }
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
                _logger.LogError(ex, "Error signing in User with authToken {0}.", model.Token ?? "{no token}");
                return Unauthorized();
            }

            return Ok(loggedInDTO);
        }

        [NonAction]
        private async Task<LoggedInDTO> GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
                new Claim(ClaimTypes.Surname, user.LastName ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(Globals.CLAIM_USER_ID, user.Id),
        };
            foreach (var role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.appSettings.JWT_KEY));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(Globals.appSettings.JWT_EXPIRE_DAYS));
            var jwt_issuer = "https://" + Globals.appSettings.HOST_NAME;
            var token = new JwtSecurityToken(
                jwt_issuer,
                jwt_issuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );
            return new LoggedInDTO
            {
                AuthToken = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                EmailId = user.Email,
                UniversityId = user.UniversityId,
                Metadata = user.Metadata
            };
        }

        [NonAction]
        public static async Task<ApplicationUser> ValidateAuth0IDToken(string idToken)
        {
            string auth0Domain = "https://" + Globals.appSettings.AUTH0_DOMAIN + "/"; // Your Auth0 domain
            string auth0Audience = Globals.appSettings.AUTH0_CLIENT_ID; // Your API Identifier

            return await ValidateIdToken(auth0Domain, auth0Audience, idToken);
        }

        [NonAction]
        public static async Task<ApplicationUser> ValidateCILogonAuthCode(string authCode, Uri callbackURL)
        {
            string cilogonDomain = "https://" + Globals.appSettings.CILOGON_DOMAIN + "/"; // Your Auth0 domain
            string cilogonClientId = Globals.appSettings.CILOGON_CLIENT_ID; // Your API Identifier
            string cilogonClientSecret = Globals.appSettings.CILOGON_CLIENT_SECRET;
            string cilogonClientToken = HttpUtility.UrlEncode(authCode);

            // Get id_token from authorization code.
            // var client = new RestClient($"{cilogonDomain}oauth2/token");
            // var request = new RestRequest(Method.Post);
            // add header and params
            // RestResponse response = client.Execute(request);

            // https://restsharp.dev/usage.html#simple-factory

            var clientOptions = new RestClientOptions
            {
                BaseUrl = new Uri(cilogonDomain)
            };
            var client = new RestClient(clientOptions,null,null, true /*Enable simple factory */);
            var request = new RestRequest("oauth2/token");
            // Todo: Are these even required?
            // https://restsharp.dev/usage.html#get-or-post
            // Put or Post ...  Also, the request will be sent as application/x-www-form-urlencoded.

            // In both cases, name and value will automatically be url - encoded.
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type=authorization_code&client_id={cilogonClientId}&client_secret={cilogonClientSecret}&code={cilogonClientToken}&redirect_uri={callbackURL}", ParameterType.RequestBody);
            
            RestResponse response = await client.ExecutePostAsync(request); // may throw exception

            var id_token = HttpUtility.UrlEncode(JObject.Parse(response.Content)["id_token"].ToString());

            return await ValidateIdToken(cilogonDomain, cilogonClientId, id_token);            
        }

        [NonAction]
        public static async Task<ApplicationUser> ValidateIdToken(string domain, string audience, string idToken)
        {
            // Validate the id_token
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{domain}.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            TokenValidationParameters validationParameters =
            new TokenValidationParameters
            {
                ValidIssuer = openIdConfig.Issuer,
                ValidAudiences = new[] { audience },
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };
            SecurityToken validatedToken;
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            var claims = handler.ValidateToken(idToken, validationParameters, out validatedToken);
            var applicationUser = new ApplicationUser
            {
                UserName = claims.FindFirstValue(ClaimTypes.Email),
                Email = claims.FindFirstValue(ClaimTypes.Email),
                FirstName = claims.FindFirstValue(ClaimTypes.GivenName) ?? "",
                LastName = claims.FindFirstValue(ClaimTypes.Surname) ?? "",
                EmailConfirmed = true
            };

            return applicationUser;
        }

        public class LoginDto
        {
            [Required]
            public string Token { get; set; }
            public AuthMethod AuthMethod { get; set; }
            public Uri CallbackURL { get; set; }
        }

        public enum AuthMethod
        {
            Auth0,
            CILogon
        }

        public class LoginAsDTO
        {
            public string emailId { get; set; }
        }
        public class LoggedInDTO
        {
            public string UserId { get; set; }
            public string UniversityId { get; set; }
            public string AuthToken { get; set; }
            public string EmailId { get; set; }
            public JObject Metadata { get; set; }
        }

        //public class CSVInstructor
        //{
        //    public string TERM { get; set; }
        //    public string SUBJ { get; set; }
        //    public string NBR { get; set; }
        //    public string SECT { get; set; }
        //    public string PRIMARY_INSTR { get; set; }
        //    public string INSTR_NETID { get; set; }
        //}

        //[HttpGet("SeedInstructors")]
        //public async Task SeedInstructors()
        //{
        //    string file = Path.Combine(Globals.appSettings.DATA_DIRECTORY, "seed", "Fall2019InstructorList.csv");
        //    TextReader reader = new StreamReader(file);
        //    var csvReader = new CsvReader(reader);
        //    var records = csvReader.GetRecords<CSVInstructor>();
        //    List<CSVInstructor> instructors = new List<CSVInstructor>(records);
        //    var filteredInstructors = instructors.ToList();
        //    foreach(var i in filteredInstructors)
        //    {
        //        var emailId = i.INSTR_NETID + "@illinois.edu";
        //        ApplicationUser applicationUser = await _userManager.FindByEmailAsync(emailId);
        //        if (applicationUser == null)
        //        {
        //            applicationUser = await _userUtils.CreateNonExistentUser(emailId);                    
        //        }
        //        if (!(await _userManager.IsInRoleAsync(applicationUser, Globals.ROLE_INSTRUCTOR)))
        //        {
        //            await _userManager.AddToRoleAsync(applicationUser, Globals.ROLE_INSTRUCTOR);
        //        }
        //    }
        //}
    }
}