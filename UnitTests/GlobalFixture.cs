using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class GlobalFixture : IDisposable
    {
        public readonly ServiceProvider _serviceProvider;
        public readonly IAuthorizationService _authorizationService;
        public readonly UserManager<ApplicationUser> _userManager;
        public readonly string _testDataDirectory = "test_data/automatically_deleted/";
        // This constructor is run once for all tests in the "Global" collection (which should be all tests)
        // https://xunit.net/docs/shared-context
        public GlobalFixture()
        {
           
            Directory.CreateDirectory(_testDataDirectory);

            _serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                // Use empty configuration for AppSettings because we do not want
                // dependencies on any environment variables or vs_appsettings.json
                .Configure<AppSettings>(new ConfigurationBuilder().Build())
                .BuildServiceProvider();

            Globals.appSettings = _serviceProvider.GetService<IOptions<AppSettings>>().Value;
            Globals.appSettings.DATA_DIRECTORY = _testDataDirectory;

            var mockAuth = new Mock<IAuthorizationService>();

            // Set up mock authorization service to always return success
            mockAuth.Setup(
                a => a.AuthorizeAsync(
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<object>(),
                            It.IsAny<string>()))
                .Returns(Task.FromResult(AuthorizationResult.Success()));

            _authorizationService = mockAuth.Object;

            var store = new Mock<IUserStore<ApplicationUser>> ();

            // No tests actually user this INSTRUCTOR1 user yet 
            // So this code is really just a demonstration for how to set up a user for testing,
            // And how to pass it into the UserManager 
            // TODO: Could also mock the user role checks too
            store.Setup(x => x.FindByIdAsync("INSTRUCTOR1@TESTLAND", CancellationToken.None))
                .ReturnsAsync(new ApplicationUser
                {
                    UserName = "INSTRUCTOR1@TESTLAND",
                    Id = "INSTRUCTOR1@TESTLAND"
                    
                });

            _userManager = new UserManager<ApplicationUser>(store.Object, null, null, null, null, null, null, null, null);

        }

        public void Dispose()
        {
            // A tiny bit safer than using Globals.appSettings.DATA_DIRECTORY
            Directory.Delete(_testDataDirectory, true);
        }
    }

    [CollectionDefinition("Global")]
    public class Global : ICollectionFixture<GlobalFixture> { }
}
