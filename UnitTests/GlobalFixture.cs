using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class GlobalFixture
    {
        public readonly ServiceProvider _serviceProvider;
        public readonly IAuthorizationService _authorizationService;

        // This constructor is run once for all tests in the "Global" collection (which should be all tests)
        // https://xunit.net/docs/shared-context
        public GlobalFixture()
        {
            _serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .Configure<AppSettings>(CTDbContext.GetConfigurations())
                .BuildServiceProvider();

            Globals.appSettings = _serviceProvider.GetService<IOptions<AppSettings>>().Value;

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
    }

    [CollectionDefinition("Global")]
    public class Global : ICollectionFixture<GlobalFixture> { }
}
