using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace UnitTests.ControllerTests
{
    [Collection("Global")]
    public class BaseControllerTest
    {
        protected readonly CTDbContext _context;
        protected readonly IAuthorizationService _authorizationService;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly UserUtils _userUtils;

        // This constructor is run before every test, ensuring a new context and in-memory DB for each test case
        // https://xunit.net/docs/shared-context
        public BaseControllerTest(GlobalFixture fixture)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CTDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .UseInternalServiceProvider(fixture._serviceProvider);

            _context = new CTDbContext(optionsBuilder.Options, null);
            _authorizationService = fixture._authorizationService;
            _userManager = fixture._userManager;
            _userUtils = new UserUtils(_userManager, _context);
        }
    }
}
