using ClassTranscribeDatabase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace UnitTests
{
    public class GlobalFixture
    {
        public ServiceProvider serviceProvider { get; private set; }

        // This constructor is run once for all tests in the "Global" collection (which should be all tests)
        // https://xunit.net/docs/shared-context
        public GlobalFixture()
        {
            serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .Configure<AppSettings>(CTDbContext.GetConfigurations())
                .BuildServiceProvider();

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
        }
    }

    [CollectionDefinition("Global")]
    public class Global : ICollectionFixture<GlobalFixture> { }
}
