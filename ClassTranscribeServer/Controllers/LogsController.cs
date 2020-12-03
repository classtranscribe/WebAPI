using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;
        public LogsController(IAuthorizationService authorizationService, CTDbContext context, UserUtils userUtils, ILogger<LogsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userUtils = userUtils;
        }

        // POST: api/Logs
        [HttpPost]
        public async Task<ActionResult> PostLog(Log log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Get Search Terms for a given offeringId
        /// Only an instructor of a course can view this.
        /// </summary>
        [HttpGet("OfferingSearchHistory")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SearchDTO>>> GetSearchLogs(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var temp = await _context.Logs.Where(l => l.OfferingId == offeringId && l.EventType == "filtertrans").ToListAsync();
            return temp.GroupBy(l => l.Json["value"].ToString())
            .Select(g => new SearchDTO
            {
                Term = g.Key,
                Count = g.Count()
            }).OrderByDescending(l => l.Count).ToList();
        }

        /// <summary>
        /// Get Search Terms for a given offeringId for a given student
        /// Only the logged in user's search history will be returned
        /// </summary>
        [HttpGet("UserSearchHistory")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SearchDTO>>> UserSearchHistory(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            // Get the user
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            // Get the user
            var user = await _userUtils.GetUser(User);
            var temp = await _context.Logs.Where(l => l.OfferingId == offeringId && l.UserId == user.Id && l.EventType == "filtertrans").ToListAsync();
            return temp.GroupBy(l => l.Json["value"].ToString())
            .Select(g => new SearchDTO
            {
                Term = g.Key,
                Count = g.Count()
            }).OrderByDescending(l => l.Count).ToList();
        }

        /// <summary>
        /// Gets all logs for the logged in user.
        /// </summary>
        [HttpGet("UserLogs")]
        [Authorize]
        public async Task<IEnumerable<Log>> GetUserLogs()
        {
            var user = await _userUtils.GetUser(User);
            if (user != null)
            {
                return await _context.Logs.Where(u => u.UserId == user.Id).ToListAsync();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets event count for a particular event type
        /// start and end are optional parameters
        /// Logs returned only for the logged in user
        /// </summary>
        [HttpGet("UserLogs/ByEvent")]
        [Authorize]
        public async Task<IEnumerable<StudentLog>> GetUserLogsByEvent(string eventType, DateTime? start = null, DateTime? end = null)
        {
            var user = await _userUtils.GetUser(User);
            if (user == null)
            {
                return null;
            }

            DateTime startTime = start ?? DateTime.Now.AddMonths(-1);
            DateTime endTime = end ?? DateTime.Now;
            var timeUpdateEvents = await _context.Logs.Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime && l.UserId == user.Id && l.EventType == eventType)
                .Select(l => new
                {
                    l.UserId,
                    l.OfferingId,
                    l.MediaId,
                    l.CreatedAt
                }).ToListAsync();

            IEnumerable<StudentLog> logs;
            if (start == null)
            {
                logs = timeUpdateEvents.GroupBy(x => x.OfferingId).Select(g => new StudentLog
                {
                    OfferingId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        LastHr = l.Where(m => m.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                        Last3days = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                        LastWeek = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                        LastMonth = l.Count(),
                    }).ToList()
                });
            }
            else
            {
                logs = timeUpdateEvents.GroupBy(x => x.OfferingId).Select(g => new StudentLog
                {
                    OfferingId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        Count = l.Count(),
                    }).ToList()
                });
            }

            return logs;
        }

        /// <summary>
        /// Gets event count for a particular event type for a particular offeringId
        /// start and end are optional parameters
        /// Logs returned only if the logged in user is an instructor for the given offeringId
        /// </summary>
        [HttpGet("CourseLogs")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseLog>>> GetCourseLogs(string offeringId, string eventType,
            DateTime? start = null, DateTime? end = null)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);

            if (offering == null)
            {
                return BadRequest();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);

            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }

            DateTime startTime = start ?? DateTime.Now.AddMonths(-1);
            DateTime endTime = end ?? DateTime.Now;

            var timeUpdateEvents = await _context.Logs
                .Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime && l.OfferingId == offeringId && l.EventType == eventType)
                .Select(l => new
                {
                    UserId = l.UserId,
                    OfferingId = l.OfferingId,
                    MediaId = l.MediaId,
                    CreatedAt = l.CreatedAt
                }).ToListAsync();

            IEnumerable<CourseLog> logs;

            if (start == null)
            {
                logs = timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
                {
                    User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Id = u.Id
                    }).FirstOrDefault(),
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        LastHr = l.Where(m => m.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                        Last3days = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                        LastWeek = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                        LastMonth = l.Count(),
                    }).ToList()
                });
            }
            else
            {
                logs = timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
                {
                    User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Id = u.Id
                    }).FirstOrDefault(),
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        Count = l.Count(),
                    }).ToList()
                });
            }

            return Ok(logs);
        }

        /// <summary>
        /// Gets all course logs for an offering
        /// Relevant issue: https://github.com/classtranscribe/WebAPI/issues/50
        /// eventType is an optional parameter (defaults to "timeupdate")
        /// Logs returned only if the logged in user is an instructor for the given offeringId
        /// </summary>
        [HttpGet("AllCourseLogs")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseLog>>> GetAllCourseLogs(string offeringId, string eventType = "timeupdate")
        {
            var offering = await _context.Offerings.FindAsync(offeringId);

            if (offering == null)
            {
                return BadRequest();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);

            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }

            var medias = await _context.Medias
                .Where(m => m.Playlist.OfferingId == offeringId)
                .ToListAsync();

            var timeUpdateEvents = await _context.Logs
                .Where(l => l.OfferingId == offeringId && l.EventType == eventType)
                .ToListAsync();

            IEnumerable<CourseLog> logs = timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
            {
                User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                {
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Id = u.Id
                }).FirstOrDefault(),
                Medias = medias.OrderBy(m => m.Index).Select(m => new MediaLog
                {
                    MediaId = m.Id,
                    MediaName = m.Name,
                    LastHr = g.Where(l => l.MediaId == m.Id && m.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                    Last3days = g.Where(l => l.MediaId == m.Id && m.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                    LastWeek = g.Where(l => l.MediaId == m.Id && m.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                    LastMonth = g.Where(l => l.MediaId == m.Id && m.CreatedAt >= DateTime.Now.AddMonths(-1)).Count(),
                    Total = g.Where(l => l.MediaId == m.Id).Count(),
                }).ToList()
            });

            return Ok(logs);
        }

        /// <summary>
        /// Gets different kinds of events
        /// Only ADMINS and ADVISORS are authorized
        /// </summary>
        [HttpGet("EventTypes")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetEventTypes()
        {
            return await _context.Logs.Select(l => l.EventType).Distinct().ToListAsync();
        }

        /// <summary>
        /// Gets unique mailIds
        /// Only ADMINS and ADVISORS are authorized
        /// </summary>
        [HttpGet("UserIds")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetUserIds()
        {
            return await _context.Users.Select(u => u.Email).Distinct().ToListAsync();
        }

        public class SearchDTO
        {
            public string Term { get; set; }
            public int Count { get; set; }
        }

        public class UserDetails
        {
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Id { get; set; }
        }

        public class CourseLog
        {
            public UserDetails User { get; set; }
            public List<MediaLog> Medias { get; set; }
        }

        public class StudentLog
        {
            public string OfferingId { get; set; }
            public List<MediaLog> Medias { get; set; }
        }

        public class MediaLog
        {
            public string MediaId { get; set; }
            public string MediaName { get; set; }
            public int LastHr { get; set; }
            public int Last3days { get; set; }
            public int LastWeek { get; set; }
            public int LastMonth { get; set; }
            public int Total { get; set; }
            public int Count { get; set; }
        }
    }
}
