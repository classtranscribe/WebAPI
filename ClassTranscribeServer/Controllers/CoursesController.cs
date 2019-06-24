using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly CTDbContext _context;

        public CoursesController(CTDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all Courses.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/Courses
        ///
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return await _context.Courses.ToListAsync();
        }

        /// <summary>
        /// Gets all Courses for departmentId
        /// </summary>
        [HttpGet("ByDepartment/{departmentId}")]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses(string departmentId)
        {
            return await _context.Courses.Where(c => c.DepartmentId == departmentId).ToListAsync();
        }

        // GET: api/Courses/
        /// <summary>
        /// Gets all Courses by Instructors for userId.
        /// </summary>
        [HttpGet("ByInstructor/{userId}")]
        public async Task<ActionResult<IEnumerable<Course>>> GetCoursesByInstructor(string userId)
        {
            return await (from c in _context.Courses
                          join co in _context.CourseOfferings on c.Id equals co.CourseId
                          join o in _context.Offerings on co.OfferingId equals o.Id
                          join uo in _context.UserOfferings on o.Id equals uo.OfferingId
                          where uo.IdentityRole.Name == "Instructor" && uo.ApplicationUserId == userId
                          select c).ToListAsync();
        }

        // GET: api/Courses/5
        /// <summary>
        /// Get course for id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(string id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return course;
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(string id, Course course)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Courses
        [HttpPost]
        public async Task<ActionResult<Course>> PostCourse(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCourse", new { id = course.Id }, course);
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Course>> DeleteCourse(string id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return course;
        }

        private bool CourseExists(string id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
