using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Xunit;
using static ClassTranscribeServer.Controllers.AccountController;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class AccountControllerTest : BaseControllerTest
    {
        private readonly AccountController _controller;

        public AccountControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new AccountController(
                (MockUserManager) fixture._serviceProvider.GetService(typeof(MockUserManager)),
                (MockSignInManager) fixture._serviceProvider.GetService(typeof(MockSignInManager)),
                _context,
                _userUtils,
                new NullLogger<AccountController>()
            )
            {
                ControllerContext = fixture._controllerContext
            };
        }

        [Fact]
        public async Task Create_User_Success()
        {
            var result = await _controller.CreateUser("example");
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_As_Success()
        {
            var result = await _controller.LoginAs(new LoginAsDTO { emailId = "example" });
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_As_Fail()
        {
            var result = await _controller.LoginAs(new LoginAsDTO { emailId = string.Empty });
            Assert.IsType<UnauthorizedResult>(result.Result);

            result = await _controller.LoginAs(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Test_Sign_In_Success()
        {
            var initial = Globals.appSettings.TEST_SIGN_IN;
            Globals.appSettings.TEST_SIGN_IN = "true";

            var result = await _controller.TestSignIn();
            Assert.IsType<OkObjectResult>(result.Result);

            Globals.appSettings.TEST_SIGN_IN = initial;
        }

        [Fact]
        public async Task Test_Sign_In_Fail()
        {
            var initial = Globals.appSettings.TEST_SIGN_IN;
            Globals.appSettings.TEST_SIGN_IN = string.Empty;

            var result = await _controller.TestSignIn();
            Assert.IsType<UnauthorizedResult>(result.Result);

            Globals.appSettings.TEST_SIGN_IN = initial;
        }

        [Fact]
        public async Task Get_User_Metadata_Success()
        {
            _context.Users.Add(new ApplicationUser
            {
                Id = TestGlobals.TEST_USER_ID,
                Metadata = new JObject(new JProperty("hello", "world"))
            });

            var result = await _controller.GetUserMetadata();
            Assert.Equal("world", result.Value.GetValue("hello"));
        }

        [Fact]
        public async Task Get_User_Metadata_Fail()
        {
            var result = await _controller.GetUserMetadata();
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Post_User_Metadata_Success()
        {
            _context.Users.Add(new ApplicationUser
            {
                Id = TestGlobals.TEST_USER_ID,
                Metadata = new JObject(new JProperty("hello", "world"))
            });

            var postResult = await _controller.PostUserMetadata(
                new JObject(new JProperty("foo", "bar"))
            );
            Assert.IsType<OkResult>(postResult);

            var getResult = await _controller.GetUserMetadata();
            Assert.Equal("bar", getResult.Value.GetValue("foo"));
            Assert.Null(getResult.Value.GetValue("hello"));
        }

        [Fact]
        public async Task Post_User_Metadata_Fail()
        {
            var result = await _controller.PostUserMetadata(new JObject());
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Sign_In_Fail()
        {
            var result = await _controller.SignIn(null);
            Assert.IsType<UnauthorizedResult>(result.Result);

            result = await _controller.SignIn(new LoginDto { });
            Assert.IsType<UnauthorizedResult>(result.Result);

            result = await _controller.SignIn(new LoginDto
            {
                AuthMethod = AuthMethod.CILogon,
                Token = "none",
                CallbackURL = new System.Uri("https://www.google.com")
            });
            Assert.IsType<UnauthorizedResult>(result.Result);
        }
    }
}