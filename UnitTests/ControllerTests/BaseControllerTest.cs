using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace UnitTests.ControllerTests
{
    public class BaseControllerTest : IDisposable
    {
        public CTDbContext _context;

        // This constructor is run before every test, ensuring a new context and in-memory DB for each test case
        // https://xunit.net/docs/shared-context
        public BaseControllerTest()
        {
            // Create a fresh service provider, and therefore a fresh InMemory database instance
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Set up CTDbContext with an in-memory database
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider);

            _context = new CTDbContext(optionsBuilder.Options, null);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
