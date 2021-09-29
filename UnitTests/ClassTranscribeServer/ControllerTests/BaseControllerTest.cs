using ClassTranscribeDatabase;
using ClassTranscribeServer.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    [Collection("Global")]
    public class BaseControllerTest
    {
        protected readonly CTDbContext _context;
        protected readonly UserUtils _userUtils;

        // This constructor is run before every test, ensuring a new context and in-memory DB for each test case
        // https://xunit.net/docs/shared-context
        public BaseControllerTest(GlobalFixture fixture)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .UseInternalServiceProvider(fixture._serviceProvider);

            _context = new CTDbContext(optionsBuilder.Options, null);
            _userUtils = new UserUtils(
                (MockUserManager) fixture._serviceProvider.GetService(typeof(MockUserManager)),
                _context
            );
        }
    }
}
