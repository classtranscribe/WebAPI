using ClassTranscribeDatabase;
using ClassTranscribeServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Security.Claims;
using Xunit;

namespace UnitTests
{
    public class GlobalFixture : IDisposable
    {
        public readonly ServiceProvider _serviceProvider;
        public readonly IAuthorizationService _authorizationService;
        public readonly ControllerContext _controllerContext;

        // 'data' must exist (and be last). Otherwise FileRecord Path setter will fail
        private static readonly string _testDataDirectory = Path.Combine("test_data","automatically_deleted","data");

        // This constructor is run once for all tests in the "Global" collection (which should be all tests)
        // https://xunit.net/docs/shared-context
        public GlobalFixture()
        {
            if (Directory.Exists(_testDataDirectory))
            {
                Directory.Delete(_testDataDirectory, true);
            }
            Directory.CreateDirectory(_testDataDirectory);

            var mockWake = new Mock<WakeDownloader>(MockBehavior.Strict, null);
            mockWake.Setup(wake => wake.UpdateVTTFile(It.IsAny<string>()));

            _serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                // Use empty configuration for AppSettings because we do not want
                // dependencies on any environment variables or vs_appsettings.json
                .Configure<AppSettings>(new ConfigurationBuilder().Build())
                .AddLogging(cfg => cfg.AddConsole())
                .AddScoped(sp => mockWake.Object)
                .AddScoped<MockUserManager>()
                .AddScoped<MockSignInManager>()
                .BuildServiceProvider();

            Globals.appSettings = _serviceProvider.GetService<IOptions<AppSettings>>().Value;
            Globals.appSettings.DATA_DIRECTORY = _testDataDirectory;
            Globals.appSettings.JWT_KEY = TestGlobals.TEST_JWT_KEY;

            var mockAuth = new Mock<IAuthorizationService>();

            // Set up mock authorization service to always return success
            mockAuth.Setup(
                auth => auth.AuthorizeAsync(
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<object>(),
                            It.IsAny<string>()))
                .ReturnsAsync(AuthorizationResult.Success());

            _authorizationService = mockAuth.Object;

            // Setup the controller context to simulate the "User" instance variable
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(Globals.CLAIM_USER_ID, TestGlobals.TEST_USER_ID),
            }, "mock"));

            _controllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = userPrincipal }
            };
        }

        public void Dispose()
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }

    [CollectionDefinition("Global")]
    public class Global : ICollectionFixture<GlobalFixture> { }
}
