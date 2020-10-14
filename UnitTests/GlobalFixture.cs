using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class GlobalFixture : IDisposable
    {
        public readonly ServiceProvider _serviceProvider;
        public readonly IAuthorizationService _authorizationService;

        // This constructor is run once for all tests in the "Global" collection (which should be all tests)
        // https://xunit.net/docs/shared-context
        public GlobalFixture()
        {
            _serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                // Use empty configuration for AppSettings because we do not want
                // dependencies on any environment variables or vs_appsettings.json
                .Configure<AppSettings>(new ConfigurationBuilder().Build())
                .BuildServiceProvider();

            Globals.appSettings = _serviceProvider.GetService<IOptions<AppSettings>>().Value;
            Globals.appSettings.DATA_DIRECTORY = "test_data/data/";
            Directory.CreateDirectory(Globals.appSettings.DATA_DIRECTORY);

            var mockAuth = new Mock<IAuthorizationService>();

            // Set up mock authorization service to always return success
            mockAuth.Setup(
                a => a.AuthorizeAsync(
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<object>(),
                            It.IsAny<string>()))
                .Returns(Task.FromResult(AuthorizationResult.Success()));

            _authorizationService = mockAuth.Object;
        }

        public void Dispose()
        {
            Directory.Delete(Globals.appSettings.DATA_DIRECTORY, true);
        }
    }

    [CollectionDefinition("Global")]
    public class Global : ICollectionFixture<GlobalFixture> { }
}
