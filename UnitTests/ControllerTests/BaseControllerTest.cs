using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace UnitTests.ControllerTests
{
    [Collection("Global")]
    public class BaseControllerTest
    {
        public CTDbContext _context;

        // This constructor is run before every test, ensuring a new context and in-memory DB for each test case
        // https://xunit.net/docs/shared-context
        public BaseControllerTest(GlobalFixture fixture)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .UseInternalServiceProvider(fixture.serviceProvider);

            _context = new CTDbContext(optionsBuilder.Options, null);
        }
    }
}
